import json
import os
import time
from datetime import datetime

import uuid

from fastapi import FastAPI, UploadFile
from fastapi.encoders import jsonable_encoder
from starlette.responses import JSONResponse

from speech_to_text import whisper_to_text
from text_to_3d import decode_dict, get_latents_multi_prompt

app = FastAPI()


@app.get("/")
async def root():
    return dict(
        audio_to_text="send audio file, receive text",
        text_to_obj="send text, receive object(s) as json",
        audio_to_obj="send audio file, receive object(s) as json",
        help="send get request to url/docs to get description of endpoints"
    )


@app.post("/text_to_obj")
def text_to_obj(prompt: str):

    task_id = uuid.uuid4()
    task_base_path = f'F:/wish3d/{str(task_id)}'
    os.mkdir(task_base_path)

    _latents, _scales = get_latents_multi_prompt(prompt, task_base_path)

    meshes = decode_dict(_latents, _scales, task_id, task_base_path)

    json_data = jsonable_encoder(meshes)
    return JSONResponse(json_data)


@app.post("/audio_to_text")
def voice_to_text(audio: UploadFile):
    return whisper_to_text(audio.file, None)[0]


@app.post("/audio_to_obj")
def voice_to_obj(
        audio: UploadFile,
        target_nr_faces: int = 5000,
):
    start = time.time()
    task_id = uuid.uuid4()
    task_base_path = f'F:/wish3d/{str(task_id)}'
    os.mkdir(task_base_path)

    text_timing = time.time()
    text = whisper_to_text(audio.file, task_base_path)[0]
    text_timing = time.time() - text_timing

    latent_timing = time.time()
    _latents, _scales = get_latents_multi_prompt(text, task_base_path)
    latent_timing = time.time() - latent_timing

    decode_timing = time.time()
    meshes = decode_dict(_latents, _scales, task_id, task_base_path, target_nr_faces)
    decode_timing = time.time() - decode_timing

    json_data = jsonable_encoder(meshes)
    end = time.time()
    with open(os.path.join(task_base_path, "timing.json"), "w+") as jf:
        json.dump(
            dict(
                id=str(task_id),
                date=str(datetime.now()),
                start=str(start),
                end=str(end),
                text_inference=f'{text_timing:.2f} seconds',
                diffusion=f'{latent_timing:.2f} seconds',
                decoding=f'{decode_timing:.2f} seconds',
                total=f'{end-start:.2f} seconds'
            ), jf
        )

    jf.close()
    return JSONResponse(json_data)

