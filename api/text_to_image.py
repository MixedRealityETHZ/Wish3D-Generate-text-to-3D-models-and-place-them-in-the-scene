import torch
import requests
from PIL import Image
from diffusers import StableDiffusionXLPipeline
import rembg

import uuid

pipeline = StableDiffusionXLPipeline.from_single_file(
    "https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0/blob/main/sd_xl_base_1.0.safetensors",
    torch_dtype=torch.float16,
    variant="fp16",
    use_safetensors=True
).to("cuda")
pipeline.enable_model_cpu_offload()


def zero_one_two_three(prompt: str, task_id: uuid.UUID, nr_samples: int = 1, resolution: int = 1024):

    images = pipeline(prompt=prompt, num_images_per_prompt=nr_samples, height=resolution, width=resolution).images

    for index, image in enumerate(images):
        no_bg = rembg.remove(image)
        print(f"Storing image at {'F:/wish3d/' + str(task_id) + '/' + str(index) + '.png'}")
        no_bg.save('F:/wish3d/' + str(task_id) + "/" + str(index) + ".png")
    return
