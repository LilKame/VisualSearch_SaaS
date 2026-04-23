# main.py
from fastapi import FastAPI, UploadFile, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from contextlib import asynccontextmanager
import onnxruntime as ort
import numpy as np
import asyncio
import time

# Ligar a IA:
# Dentro da pasta ai-engine, com o venv ativado
# uvicorn main:app --host 0.0.0.0 --port 8000 --reload

from preprocess import preprocess_image, l2_normalize

MODEL_PATH = "./models/clip_vit_b32.onnx"

# Variável global — a sessão ONNX é pesada, carregamos só uma vez
session: ort.InferenceSession = None

@asynccontextmanager
async def lifespan(app: FastAPI):
    """
    Executado na inicialização e encerramento do serviço.
    Carrega o modelo ONNX em memória uma única vez.
    """
    global session

    print("Carregando modelo CLIP...")
    opts = ort.SessionOptions()
    opts.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL
    opts.intra_op_num_threads = 4

    # Tenta GPU primeiro, cai para CPU se não tiver
    providers = ["CUDAExecutionProvider", "CPUExecutionProvider"]
    session = ort.InferenceSession(MODEL_PATH, sess_options=opts, providers=providers)

    # Warm-up: primeira inferência é sempre mais lenta
    print("Aquecendo modelo (warm-up)...")
    dummy = np.zeros((1, 3, 224, 224), dtype=np.float32)
    session.run(None, {"pixel_values": dummy})

    provider_usado = session.get_providers()[0]
    print(f"Modelo pronto. Usando: {provider_usado}")
    yield

    session = None
    print("Modelo descarregado.")


app = FastAPI(
    title="VisualSearch AI Engine",
    version="1.0.0",
    lifespan=lifespan
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["POST", "GET"],
    allow_headers=["*"],
)

# Limita inferências simultâneas — protege a CPU/GPU
inference_semaphore = asyncio.Semaphore(4)


def run_inference(image_bytes: bytes) -> tuple[list[float], float]:
    """
    Executa a inferência ONNX e retorna (embedding, tempo_ms).
    Separado em função própria para rodar no executor (não bloqueia o event loop).
    """
    pixel_values = preprocess_image(image_bytes)

    start = time.perf_counter()
    outputs = session.run(None, {"pixel_values": pixel_values})
    elapsed_ms = (time.perf_counter() - start) * 1000

    # outputs[1] é o pooler_output — vetor global que representa a imagem inteira
    embedding = l2_normalize(outputs[1][0])

    return embedding, round(elapsed_ms, 2)


@app.get("/health")
async def health():
    """Verifica se o serviço e o modelo estão prontos."""
    return {
        "status": "ok",
        "model_loaded": session is not None,
        "provider": session.get_providers()[0] if session else None
    }


@app.post("/embed")
async def extract_embedding(file: UploadFile):
    """
    Recebe uma imagem e retorna o vetor de 512 dimensões (embedding).
    Esse vetor é o que vai ser salvo no Qdrant.
    """
    # Valida tipo do arquivo
    TIPOS_ACEITOS = {"image/jpeg", "image/png", "image/webp"}
    if file.content_type not in TIPOS_ACEITOS:
        raise HTTPException(
            status_code=422,
            detail=f"Formato não suportado: {file.content_type}. Use JPEG, PNG ou WEBP."
        )

    contents = await file.read()

    # Valida tamanho (máx 15MB)
    if len(contents) > 15 * 1024 * 1024:
        raise HTTPException(
            status_code=413,
            detail="Imagem muito grande. Máximo permitido: 15 MB."
        )

    try:
        # Roda a inferência fora do event loop (não trava outras requisições)
        async with inference_semaphore:
            loop = asyncio.get_event_loop()
            embedding, inference_ms = await loop.run_in_executor(
                None, run_inference, contents
            )
    except Exception as e:
        raise HTTPException(
            status_code=422,
            detail=f"Não foi possível processar a imagem: {str(e)}"
        )

    return {
        "embedding": embedding,
        "dimensions": len(embedding),
        "inference_ms": inference_ms
    }


@app.post("/embed-batch")
async def extract_embeddings_batch(files: list[UploadFile]):
    """
    Processa múltiplas imagens de uma vez.
    Usado na catalogação quando o produto tem várias fotos (frente, verso, topo).
    """
    if len(files) > 10:
        raise HTTPException(status_code=400, detail="Máximo de 10 imagens por lote.")

    resultados = []
    for file in files:
        contents = await file.read()
        try:
            async with inference_semaphore:
                loop = asyncio.get_event_loop()
                embedding, ms = await loop.run_in_executor(
                    None, run_inference, contents
                )
            resultados.append({"embedding": embedding, "inference_ms": ms, "error": None})
        except Exception as e:
            resultados.append({"embedding": None, "inference_ms": 0, "error": str(e)})

    return {"results": resultados, "total": len(resultados)}