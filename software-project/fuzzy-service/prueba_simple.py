import asyncio
from fastapi import FastAPI
from fastapi.testclient import TestClient
from kink import di
from medyator import Medyator, Command, CommandHandler
import medyator.kink  # activa di.add_medyator()

# ------------------------
# Request (Command)
# ------------------------
class CreateUserCommand(Command):
    def __init__(self, name: str, email: str):
        self.name = name
        self.email = email
        self.result = None  # aquí guardaremos el DTO creado


# ------------------------
# Handler
# ------------------------
class CreateUserHandler(CommandHandler[CreateUserCommand]):
    async def __call__(self, request: CreateUserCommand) -> None:
        # simula la creación de la entidad
        user = {"id": 1, "name": request.name, "email": request.email}
        print(f"[Handler] Usuario creado: {user}")
        # asignamos el resultado al propio request
        request.result = user


# ------------------------
# App y DI
# ------------------------
app = FastAPI()

def configure_di():
    di.add_medyator()
    di[CreateUserCommand] = CreateUserHandler()

configure_di()


@app.post("/users")
async def create_user(name: str, email: str):
    mediator: Medyator = di[Medyator]
    cmd = CreateUserCommand(name=name, email=email)
    await mediator.send(cmd)
    # ahora el resultado vive en cmd.result
    return cmd.result


# ------------------------
# Test rápido con TestClient
# ------------------------
if __name__ == "__main__":
    with TestClient(app) as client:
        resp = client.post("/users", params={"name": "Alice", "email": "alice@example.com"})
        print("=" * 40)
        print("Respuesta del endpoint:")
        print(resp.json())
