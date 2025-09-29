"""
Error handling middleware for FastAPI.
- Catches unhandled exceptions
- Maps known domain/application exceptions to HTTP status codes
- Produces consistent JSON error responses
"""
from __future__ import annotations

import logging
import traceback
import uuid
from typing import Callable

from fastapi import Request, Response
from fastapi.responses import JSONResponse
from starlette.middleware.base import BaseHTTPMiddleware
from pydantic import ValidationError as PydanticValidationError

from FuzzyService.Domain.Errors.DomainErrors import (
    DuplicateEntityError,
    EntityNotFoundError,
    ValidationError,
    BusinessRuleViolationError
)

_logger = logging.getLogger(__name__)


class ErrorHandlingMiddleware(BaseHTTPMiddleware):
    async def dispatch(self, request: Request, call_next: Callable) -> Response:
        request_id = str(uuid.uuid4())
        try:
            response = await call_next(request)
            return response
        except DuplicateEntityError as exc:
            status_code = 409
            code = "DuplicateEntity"
            message = str(exc)
            _logger.warning(
                "Duplicate entity error [request_id=%s]: %s",
                request_id,
                message,
            )
            return JSONResponse(
                status_code=status_code,
                content={
                    "error": {
                        "code": code,
                        "message": message,
                        "requestId": request_id,
                        "path": request.url.path,
                    }
                },
            )
        except EntityNotFoundError as exc:
            status_code = 404
            code = "EntityNotFound"
            message = str(exc)
            _logger.warning(
                "Entity not found [request_id=%s]: %s",
                request_id,
                message,
            )
            return JSONResponse(
                status_code=status_code,
                content={
                    "error": {
                        "code": code,
                        "message": message,
                        "requestId": request_id,
                        "path": request.url.path,
                    }
                },
            )
        except ValidationError as exc:
            status_code = 422
            code = "ValidationError"
            message = str(exc)
            _logger.warning(
                "Validation error [request_id=%s]: %s",
                request_id,
                message,
            )
            return JSONResponse(
                status_code=status_code,
                content={
                    "error": {
                        "code": code,
                        "message": message,
                        "requestId": request_id,
                        "path": request.url.path,
                    }
                },
            )
        except BusinessRuleViolationError as exc:
            status_code = 400
            code = "BusinessRuleViolation"
            message = str(exc)
            _logger.warning(
                "Business rule violation [request_id=%s]: %s",
                request_id,
                message,
            )
            return JSONResponse(
                status_code=status_code,
                content={
                    "error": {
                        "code": code,
                        "message": message,
                        "requestId": request_id,
                        "path": request.url.path,
                    }
                },
            )
        except PydanticValidationError as exc:
            status_code = 422
            code = "ValidationError"
            # Extract first error message for simplicity
            error_details = exc.errors()[0] if exc.errors() else {}
            message = error_details.get('msg', str(exc))
            _logger.warning(
                "Pydantic validation error [request_id=%s]: %s",
                request_id,
                message,
            )
            return JSONResponse(
                status_code=status_code,
                content={
                    "error": {
                        "code": code,
                        "message": message,
                        "requestId": request_id,
                        "path": request.url.path,
                    }
                },
            )
        except Exception as exc:  # noqa: BLE001 - central catch
            # Map to status code: by default 500
            status_code = 500
            code = exc.__class__.__name__
            message = str(exc) or "Internal Server Error"

            # Log with traceback and request metadata
            _logger.exception(
                "Unhandled exception [request_id=%s] %s: %s\n%s",
                request_id,
                code,
                message,
                traceback.format_exc(),
            )

            return JSONResponse(
                status_code=status_code,
                content={
                    "error": {
                        "code": code,
                        "message": message,
                        "requestId": request_id,
                        "path": request.url.path,
                    }
                },
            )
