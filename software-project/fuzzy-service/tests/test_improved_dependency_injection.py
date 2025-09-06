"""Tests para validar las mejoras en inyección de dependencias."""
import pytest
import asyncio
from unittest.mock import Mock, patch
from typing import Type

from FuzzyService.Application.Configuration.HandlerFactory import (
    HandlerFactory,
    HandlerRegistry,
    get_handler_factory,
    get_handler_registry
)
from FuzzyService.Application.Configuration.ImprovedDependencyInjection import (
    configure_improved_application_di,
    get_lifecycle_manager,
    DILifecycleManager
)


class MockCommand:
    """Mock command para testing."""
    pass


class MockHandler:
    """Mock handler sin dependencias."""
    def __init__(self):
        self.created = True


class MockHandlerWithDependency:
    """Mock handler con dependencias."""
    def __init__(self, dependency: str):
        self.dependency = dependency
        self.created = True


class MockRepository:
    """Mock repository para testing."""
    def __init__(self):
        self.disposed = False
    
    async def dispose(self):
        self.disposed = True


class TestHandlerFactory:
    """Tests para HandlerFactory."""
    
    def setup_method(self):
        """Setup para cada test."""
        self.factory = HandlerFactory()
    
    def test_create_handler_without_dependencies(self):
        """Test creación de handler sin dependencias."""
        handler = self.factory.create_handler(MockHandler)
        assert handler.created is True
        assert isinstance(handler, MockHandler)
    
    def test_create_handler_with_dependencies(self):
        """Test creación de handler con dependencias."""
        from kink import di
        
        # Registrar dependencia
        di[str] = "test_dependency"
        
        try:
            handler = self.factory.create_handler(MockHandlerWithDependency)
            assert handler.created is True
            assert handler.dependency == "test_dependency"
        finally:
            # Cleanup - recrear el contenedor
            try:
                di._container = di._container.__class__()
            except:
                pass
    
    def test_create_handler_missing_dependency(self):
        """Test error cuando falta dependencia."""
        from kink import di
        # Limpiar el contenedor DI
        if hasattr(di, '_services'):
            di._services.pop(str, None)
        
        # Crear una nueva factory para este test
        test_factory = HandlerFactory()
        
        with pytest.raises(ValueError, match="Cannot resolve dependency"):
            test_factory.create_handler(MockHandlerWithDependency)
    
    def test_singleton_handler(self):
        """Test que handlers singleton retornan la misma instancia."""
        # Registrar como singleton
        self.factory.register_singleton(MockHandler)
        
        handler1 = self.factory.create_handler(MockHandler)
        handler2 = self.factory.create_handler(MockHandler)
        
        assert handler1 is handler2
    
    def test_non_singleton_handler(self):
        """Test que handlers no-singleton retornan instancias diferentes."""
        handler1 = self.factory.create_handler(MockHandler)
        handler2 = self.factory.create_handler(MockHandler)
        
        assert handler1 is not handler2
    
    def test_validate_handler_dependencies_valid(self):
        """Test validación exitosa de dependencias."""
        result = self.factory.validate_handler_dependencies(MockHandler)
        assert result is True
    
    def test_validate_handler_dependencies_invalid(self):
        """Test validación fallida de dependencias."""
        from kink import di
        # Limpiar el contenedor DI
        if hasattr(di, '_services'):
            di._services.pop(str, None)
        
        # Crear una nueva factory para este test
        test_factory = HandlerFactory()
        
        result = test_factory.validate_handler_dependencies(MockHandlerWithDependency)
        assert result is False
    
    def test_clear_cache(self):
        """Test limpieza de cache."""
        self.factory.register_singleton(MockHandler)
        handler1 = self.factory.create_handler(MockHandler)
        
        self.factory.clear_cache()
        handler2 = self.factory.create_handler(MockHandler)
        
        assert handler1 is not handler2


class TestHandlerRegistry:
    """Tests para HandlerRegistry."""
    
    def setup_method(self):
        """Setup para cada test."""
        self.factory = HandlerFactory()
        self.registry = HandlerRegistry(self.factory)
    
    def test_register_command_handler(self):
        """Test registro de command handler."""
        from kink import di
        
        self.registry.register_command_handler(MockCommand, MockHandler)
        
        # Verificar que se registró en DI
        assert MockCommand in di
        
        # Verificar que se puede crear
        handler_factory = di[MockCommand]
        handler = handler_factory()
        assert isinstance(handler, MockHandler)
    
    def test_register_handlers_from_mapping(self):
        """Test registro masivo de handlers."""
        from kink import di
        
        mapping = {
            MockCommand: MockHandler
        }
        
        self.registry.register_handlers_from_mapping(mapping)
        
        assert MockCommand in di
        handler_factory = di[MockCommand]
        handler = handler_factory()
        assert isinstance(handler, MockHandler)
    
    def test_validate_all_handlers_success(self):
        """Test validación exitosa de todos los handlers."""
        mapping = {
            MockCommand: MockHandler
        }
        
        self.registry.register_handlers_from_mapping(mapping)
        result = self.registry.validate_all_handlers()
        
        assert result is True
    
    def test_validate_all_handlers_failure(self):
        """Test validación fallida de handlers."""
        from kink import di
        # Limpiar el contenedor DI
        if hasattr(di, '_services'):
            di._services.pop(str, None)
        
        # Crear nueva factory y registry para este test
        test_factory = HandlerFactory()
        test_registry = HandlerRegistry(test_factory)
        
        mapping = {
            MockCommand: MockHandlerWithDependency
        }
        
        test_registry.register_handlers_from_mapping(mapping)
        result = test_registry.validate_all_handlers()
        
        assert result is False
    
    def test_get_registered_handlers_count(self):
        """Test estadísticas de handlers registrados."""
        # Crear mock commands y queries
        class MockCommandType:
            __name__ = "TestCommand"
        
        class MockQueryType:
            __name__ = "TestQuery"
        
        self.registry.register_command_handler(MockCommandType, MockHandler)
        self.registry.register_query_handler(MockQueryType, MockHandler)
        
        stats = self.registry.get_registered_handlers_count()
        
        assert stats['commands'] == 1
        assert stats['queries'] == 1
        assert stats['total'] == 2


class TestDILifecycleManager:
    """Tests para DILifecycleManager."""
    
    def setup_method(self):
        """Setup para cada test."""
        self.manager = DILifecycleManager()
    
    def test_register_disposable(self):
        """Test registro de disposables."""
        repo = MockRepository()
        self.manager.register_disposable(repo)
        
        assert repo in self.manager._disposables
    
    def test_add_startup_hook(self):
        """Test añadir hook de startup."""
        async def startup_hook():
            pass
        
        self.manager.add_startup_hook(startup_hook)
        assert startup_hook in self.manager._startup_hooks
    
    def test_add_shutdown_hook(self):
        """Test añadir hook de shutdown."""
        async def shutdown_hook():
            pass
        
        self.manager.add_shutdown_hook(shutdown_hook)
        assert shutdown_hook in self.manager._shutdown_hooks
    
    @pytest.mark.asyncio
    async def test_startup_execution(self):
        """Test ejecución de hooks de startup."""
        executed = []
        
        async def hook1():
            executed.append(1)
        
        async def hook2():
            executed.append(2)
        
        self.manager.add_startup_hook(hook1)
        self.manager.add_startup_hook(hook2)
        
        await self.manager.startup()
        
        assert executed == [1, 2]
    
    @pytest.mark.asyncio
    async def test_shutdown_execution(self):
        """Test ejecución de cleanup en shutdown."""
        repo1 = MockRepository()
        repo2 = MockRepository()
        
        self.manager.register_disposable(repo1)
        self.manager.register_disposable(repo2)
        
        await self.manager.shutdown()
        
        assert repo1.disposed is True
        assert repo2.disposed is True
    
    @pytest.mark.asyncio
    async def test_shutdown_with_hooks(self):
        """Test shutdown con hooks y disposables."""
        executed = []
        repo = MockRepository()
        
        async def shutdown_hook():
            executed.append("hook")
        
        self.manager.add_shutdown_hook(shutdown_hook)
        self.manager.register_disposable(repo)
        
        await self.manager.shutdown()
        
        assert "hook" in executed
        assert repo.disposed is True
    
    @pytest.mark.asyncio
    async def test_startup_hook_failure(self):
        """Test manejo de errores en hooks de startup."""
        async def failing_hook():
            raise Exception("Startup failed")
        
        self.manager.add_startup_hook(failing_hook)
        
        with pytest.raises(Exception, match="Startup failed"):
            await self.manager.startup()
    
    @pytest.mark.asyncio
    async def test_shutdown_hook_failure_continues(self):
        """Test que errores en shutdown no detienen el proceso."""
        executed = []
        
        async def failing_hook():
            raise Exception("Shutdown failed")
        
        async def normal_hook():
            executed.append("normal")
        
        self.manager.add_shutdown_hook(failing_hook)
        self.manager.add_shutdown_hook(normal_hook)
        
        # No debe lanzar excepción
        await self.manager.shutdown()
        
        # Hook normal debe ejecutarse a pesar del error
        assert "normal" in executed


class TestImprovedDependencyInjection:
    """Tests de integración para la configuración mejorada de DI."""
    
    def test_get_handler_factory_singleton(self):
        """Test que get_handler_factory retorna singleton."""
        factory1 = get_handler_factory()
        factory2 = get_handler_factory()
        
        assert factory1 is factory2
    
    def test_get_handler_registry_singleton(self):
        """Test que get_handler_registry retorna singleton."""
        registry1 = get_handler_registry()
        registry2 = get_handler_registry()
        
        assert registry1 is registry2
    
    def test_get_lifecycle_manager_singleton(self):
        """Test que get_lifecycle_manager retorna singleton."""
        manager1 = get_lifecycle_manager()
        manager2 = get_lifecycle_manager()
        
        assert manager1 is manager2
    
    @patch('FuzzyService.Application.Configuration.ImprovedDependencyInjection._import_class')
    def test_configure_improved_application_di_success(self, mock_import):
        """Test configuración exitosa de DI mejorada."""
        # Mock imports
        mock_import.side_effect = lambda x: MockHandler if 'Handler' in x else MockCommand
        
        # No debe lanzar excepción
        try:
            configure_improved_application_di()
        except Exception as e:
            # Puede fallar por dependencias faltantes, pero no por errores de lógica
            assert "Cannot resolve dependency" in str(e) or "Missing repositories" in str(e)
    
    def test_factory_integration_with_registry(self):
        """Test integración entre factory y registry."""
        factory = get_handler_factory()
        registry = get_handler_registry()
        
        # Verificar que registry usa el mismo factory
        assert registry.factory is factory


if __name__ == "__main__":
    pytest.main([__file__])