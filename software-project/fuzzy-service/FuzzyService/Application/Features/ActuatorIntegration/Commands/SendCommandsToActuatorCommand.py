from typing import List, Dict, Any
from medyator import Command


class SendCommandsToActuatorCommand(Command):
    """
    Comando para enviar comandos al actuator-service.

    Formato del payload:
    {
      "commands": [
        { "actuatorCode": "Ventiladores", "dutyCycle": 75.0, "duration": 195.0 },
        { "actuatorCode": "CalefactorAgua", "power": "ON", "duration": 193.0 }
      ]
    }
    """

    def __init__(self, commands: List[Dict[str, Any]]):
        self.commands = commands
