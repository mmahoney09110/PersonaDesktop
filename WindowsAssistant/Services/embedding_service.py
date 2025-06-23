from fastapi import FastAPI
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer
from typing import List
import uvicorn

app = FastAPI()
model = SentenceTransformer("all-MiniLM-L6-v2")

class TextRequest(BaseModel):
    text: str

@app.post("/embed")
def embed_text(req: TextRequest):
    embedding = model.encode(req.text)
    return {"embedding": embedding.tolist()}

if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8000)
