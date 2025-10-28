import { NextPageContext } from 'next';
import Link from 'next/link';

interface ErrorProps {
  statusCode?: number;
  hasGetInitialProps?: boolean;
  err?: Error;
}

function ErrorPage({ statusCode }: ErrorProps) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-md w-full text-center">
        <div className="mb-8">
          <h1 className="text-6xl font-bold text-gray-900 mb-4">
            {statusCode || 'Error'}
          </h1>
          <h2 className="text-2xl font-semibold text-gray-700 mb-2">
            {statusCode === 404
              ? 'Página no encontrada'
              : 'Ha ocurrido un error'}
          </h2>
          <p className="text-gray-600">
            {statusCode === 404
              ? 'Lo sentimos, la página que buscas no existe.'
              : 'Ha ocurrido un error inesperado.'}
          </p>
        </div>
        
        <div className="space-y-4">
          <Link
            href="/"
            className="inline-block bg-green-600 text-white px-6 py-3 rounded-md hover:bg-green-700 transition duration-300"
          >
            Volver al inicio
          </Link>
          
          <div>
            <Link
              href="/dashboard"
              className="text-green-600 hover:text-green-800 underline"
            >
              Ir al dashboard
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}

ErrorPage.getInitialProps = ({ res, err }: NextPageContext) => {
  const statusCode = res ? res.statusCode : err ? err.statusCode : 404;
  return { statusCode };
};

export default ErrorPage;