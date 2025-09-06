import asyncio
from kink import di
from medyator import Medyator

# Importar configuración DI
from FuzzyService.Application.Configuration.DependencyInjection import configure_application_di
from FuzzyService.Infrastructure.Configuration.DependencyInjection import configure_infrastructure_di

# Importar comando y handler
from FuzzyService.Application.Features.FuzzySystems.Commands.CreateFuzzySystem.CreateFuzzySystemCommand import CreateFuzzySystemCommand
from FuzzyService.Application.Features.FuzzySystems.Commands.CreateFuzzySystem.CreateFuzzySystemHandler import CreateFuzzySystemHandler

async def test_mediator():
    print("Configurando DI...")
    configure_infrastructure_di()
    configure_application_di()
    
    # Importar y ejecutar startup para registrar repositorios
    from FuzzyService.Infrastructure.Configuration.DependencyInjection import on_startup
    print("Ejecutando startup...")
    await on_startup()
    
    print("Creando comando...")
    command = CreateFuzzySystemCommand(name="Test System", isActive=True)
    print(f"Comando creado: {command}")
    print(f"_result inicial: {command._result}")
    
    print("Obteniendo mediador...")
    mediator: Medyator = di[Medyator]
    print(f"Mediador obtenido: {mediator}")
    
    print("Enviando comando...")
    try:
        await mediator.send(command)
        print("Comando enviado exitosamente")
        print(f"_result después del send: {command._result}")
        print(f"Tipo de _result: {type(command._result)}")
    except Exception as e:
        print(f"Error enviando comando: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    asyncio.run(test_mediator())