# preprocess.py
import numpy as np
from PIL import Image
import io

# Esses valores são o padrão do CLIP — não altere
CLIP_MEAN = np.array([0.48145466, 0.4578275,  0.40821073], dtype=np.float32)
CLIP_STD  = np.array([0.26862954, 0.26130258, 0.27577711], dtype=np.float32)

def preprocess_image(image_bytes: bytes) -> np.ndarray:
    """
    Transforma bytes de imagem em tensor (1, 3, 224, 224) pronto para o CLIP.
    O CLIP sempre espera imagens 224x224 normalizadas com esses valores fixos.
    """
    img = Image.open(io.BytesIO(image_bytes)).convert("RGB")
    img = img.resize((224, 224), Image.LANCZOS)

    arr = np.array(img, dtype=np.float32) / 255.0          # [0,1]
    arr = (arr - CLIP_MEAN) / CLIP_STD                      # normaliza
    arr = arr.transpose(2, 0, 1)                            # HWC → CHW
    return arr[np.newaxis, ...]                              # adiciona batch dim: (1,3,224,224)

def l2_normalize(vector: np.ndarray) -> list[float]:
    """
    Normaliza o vetor para que a distância coseno funcione corretamente no Qdrant.
    Sem isso, vetores de imagens maiores teriam peso maior injustamente.
    """
    norm = np.linalg.norm(vector)
    if norm == 0:
        return vector.tolist()
    return (vector / norm).tolist()