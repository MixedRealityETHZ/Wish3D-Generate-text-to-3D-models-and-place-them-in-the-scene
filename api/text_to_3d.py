import json
import os
import zipfile
from io import BytesIO
from typing import List
from uuid import UUID

import pymeshlab as ml
import numpy as np
import torch
from fastapi.responses import Response

from shap_e.diffusion.sample import sample_latents
from shap_e.diffusion.gaussian_diffusion import diffusion_from_config
from shap_e.models.download import load_model, load_config
from shap_e.util.notebooks import decode_latent_mesh
from text_utils import split_prompt

GUIDANCE_SCALE = 15.
ZIP_SUB_DIRECTORY = "archive"

device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
print(f"If running for the first time, model download will take several minutes, device: {device}")


# device = 'cuda'
transmitter = load_model('transmitter', device=device)
text_to_geom_model = load_model('text300M', device=device)
diffusion = diffusion_from_config(load_config('diffusion'))

print(f"Models are loaded, API available momentarily, device: {device}")


def get_latents(prompt: str, nr_samples: int):
    _prompt = [prompt] * nr_samples
    _batch_size = len(_prompt)
    with torch.no_grad():
        _latents = sample_latents(
            batch_size=_batch_size,
            model=text_to_geom_model,
            diffusion=diffusion,
            guidance_scale=GUIDANCE_SCALE,
            model_kwargs=dict(texts=_prompt),
            progress=True,
            clip_denoised=True,
            use_fp16=True,
            use_karras=True,
            karras_steps=32,
            sigma_min=1e-3,
            sigma_max=160.,
            s_churn=0,
        )
    return _latents


def get_latents_multi_prompt(prompt: str, task_base_path: str):

    _prompt, _scales = split_prompt(prompt, task_base_path)
    _batch_size = len(_prompt)
    with torch.no_grad():
        _latents = sample_latents(
            batch_size=_batch_size,
            model=text_to_geom_model,
            diffusion=diffusion,
            guidance_scale=GUIDANCE_SCALE,
            model_kwargs=dict(texts=_prompt),
            progress=True,
            clip_denoised=True,
            use_fp16=True,
            use_karras=True,
            karras_steps=32,
            sigma_min=1e-3,
            sigma_max=160.,
            s_churn=0,
        )
    return _latents, _scales


def decode_latents_to_files(latents: torch.Tensor) -> List:
    filenames = []
    for _i, _latent in enumerate(latents):
        with open(f'sample_{_i}.obj', 'w') as f:
            t = decode_latent_mesh(transmitter, _latent).tri_mesh()
            t.write_obj(f)
        f.close()
        filenames.append(f'sample_{_i}.obj')

    return filenames


def decode_dict(latents: torch.Tensor, scales: List[float], task_id: UUID, task_base_path: str, target_nr_faces: int = 1000):
    meshes = []
    for i in range(latents.shape[0]):
        t = decode_latent_mesh(transmitter, latents[i])
        R = t.vertex_channels["R"].cpu().numpy()
        G = t.vertex_channels["G"].cpu().numpy()
        B = t.vertex_channels["B"].cpu().numpy()

        rgba_matrix = np.column_stack((R, G, B, np.ones_like(R)))

        m = ml.Mesh(
            vertex_matrix=t.verts[:, [1, 2, 0]].cpu().numpy(),
            face_matrix=t.faces.cpu().numpy(),
            v_color_matrix=rgba_matrix
        )

        ms = ml.MeshSet()
        ms.add_mesh(m, mesh_name="mesh")
        # ms.apply_filter('meshing_decimation_quadric_edge_collapse', targetfacenum=target_nr_faces)
        ms.save_current_mesh("test.obj")
        v = np.array(ms.current_mesh().vertex_matrix())
        f = np.array(ms.current_mesh().face_matrix())
        # f[:, [0, 2]] = f[:, [2, 0]]
        # n = np.array(ms.current_mesh().face_normal_matrix())
        vertex_color_matrix = np.array(ms.current_mesh().vertex_color_matrix())

        meshes.append(
            dict(
                UUID=str(task_id) + '-' + str(i),
                Scale=scales[i],
                Mesh=dict(
                    Vertices=dict(
                        X=v[:, 0].tolist(),
                        Y=v[:, 1].tolist(),
                        Z=v[:, 2].tolist()
                    ),
                    R=vertex_color_matrix[:, 0].tolist(),
                    G=vertex_color_matrix[:, 1].tolist(),
                    B=vertex_color_matrix[:, 2].tolist(),
                    # Normals=n.tolist(),
                ),
                Triangles=f.flatten().tolist()
            )
        )
    if task_base_path:
        with open(os.path.join(task_base_path, "meshes.json"), 'w+') as jf:
            json.dump(meshes, jf)
        jf.close()
    return meshes


def decode_single_file(latents: torch.Tensor, filename: str):

    t = decode_latent_mesh(transmitter, latents[0]).tri_mesh()

    colors = []
    # for i in range(len(t.verts)):
    #     colors.append([
    #         t.vertex_channels["R"][i],
    #         t.vertex_channels["G"][i],
    #         t.vertex_channels["B"][i],
    #         1.0
    #     ])
    m = ml.Mesh(t.verts[:, [1, 2, 0]], t.faces)
    # vertex_colors = np.stack([t.vertex_channels[x] for x in "RGB"], axis=1)
    # vertices = [
    #     "{} {} {} {} {} {}".format(*coord, *color)
    #     for coord, color in zip(t.verts.tolist(), vertex_colors.tolist())
    # ]
    # faces = [
    #     "f {} {} {}".format(str(tri[0] + 1), str(tri[1] + 1), str(tri[2] + 1))
    #     for tri in t.faces.tolist()
    # ]


    ms = ml.MeshSet()
    ms.add_mesh(m, mesh_name="mesh")
    ms.apply_filter('meshing_decimation_quadric_edge_collapse', targetfacenum=1000)

    ms.save_current_mesh(filename)
    return filename


def decode_latents_to_mesh(latents: torch.Tensor):
    meshes = {}
    for _i, _latent in enumerate(latents):
        t = decode_latent_mesh(transmitter, _latent).tri_mesh()
        meshes[str(_i)] = {
            "v": t.verts.flatten(),
            "f": t.faces.flatten(),
            "r": t.vertex_channels['R'].flatten(),
            "g": t.vertex_channels['G'].flatten(),
            "b": t.vertex_channels['B'].flatten()
        }
    return meshes


def create_zipped_response(filenames):
    # Open StringIO to grab in-memory ZIP contents
    s = BytesIO()
    # The zip compressor
    zf = zipfile.ZipFile(s, "w")

    for fpath in filenames:
        print(fpath)
        # Calculate path for file in zip
        fdir, fname = os.path.split(fpath)
        zip_path = os.path.join(ZIP_SUB_DIRECTORY, fname)

        # Add file, at correct path
        zf.write(fpath, zip_path)

    # Must close zip for all contents to be written
    zf.close()

    # Grab ZIP file from in-memory, make response with correct MIME-type
    resp = Response(s.getvalue(), media_type="application/x-zip-compressed")
    # ..and correct content-disposition

    return resp

