# Execute esse script apenas uma vez;
# Execute apenas uma vez "py -m pip install torch transformers onnxscript"

import torch
from transformers import CLIPModel
import os

print("Baixando modelo CLIP...")
model = CLIPModel.from_pretrained("openai/clip-vit-base-patch32")
model.eval()

# mostra onde está salvando
print("Diretório atual:", os.getcwd())

# cria pasta models se não existir
os.makedirs("models", exist_ok=True)

dummy_input = torch.zeros(1, 3, 224, 224)

print("Exportando para ONNX...")
torch.onnx.export(
    model.vision_model,
    dummy_input,
    "models/clip_vit_b32.onnx",
    input_names=["pixel_values"],
    output_names=["last_hidden_state", "pooler_output"],
    dynamic_axes={"pixel_values": {0: "batch_size"}},
    opset_version=14,
)

print("Modelo salvo com sucesso!")