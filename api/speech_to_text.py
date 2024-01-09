import os
from typing import Union

import torch

from transformers import WhisperProcessor, WhisperForConditionalGeneration
import librosa
import soundfile


SAMPLERATE = 16000
device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
print(f"Setting up whisper, device: {device}")

processor = WhisperProcessor.from_pretrained("openai/whisper-large-v2", device=device)
model = WhisperForConditionalGeneration.from_pretrained("openai/whisper-large-v2").to(device)
model.config.forced_decoder_ids = None

print("whisper setup done")


def whisper_to_text(audio_file, task_base_path: Union[str, None]):
    with torch.no_grad():
        # load model and processor
        data, samplerate = librosa.load(audio_file, sr=SAMPLERATE)
        if task_base_path:
            soundfile.write(os.path.join(task_base_path, "prompt.wav"), data, int(samplerate), subtype='PCM_24')

        input_features = processor(data, sampling_rate=SAMPLERATE, return_tensors="pt").input_features

        predicted_ids = model.generate(input_features.to(device))

        transcription = processor.batch_decode(predicted_ids, skip_special_tokens=True)
    return transcription
