import sys
import os
from TTS.api import TTS

def main():
    if len(sys.argv) < 3:
        print("Usage: generate_tts.py \"Your text here\" output.wav")
        return

    text = sys.argv[1]
    output_path = sys.argv[2]

    # model
    model_name = "tts_models/en/ljspeech/tacotron2-DDC"

    # Load TTS
    tts = TTS(model_name=model_name, progress_bar=False, gpu=False)

    # Generate speech
    tts.tts_to_file(text=text, file_path=output_path)
    print(f"Saved TTS audio to: {output_path}")

if __name__ == "__main__":
    main()
