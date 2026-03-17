'use client';

import React from 'react';

interface TableColumn {
  key: string;
  label: string;
  render?: (value: unknown, row: unknown) => React.ReactNode;
  className?: string;
}

interface TableProps {
  columns: TableColumn[];
  data: unknown[];
  className?: string;
  headerClassName?: string;
  rowClassName?: string;
  emptyMessage?: string;
  responsive?: boolean;
  mobileCardRender?: (item: unknown) => React.ReactNode;
}

const Table = React.memo(function Table({
  columns,
  data,
  className = '',
  headerClassName = 'bg-gray-50',
  rowClassName = '',
  emptyMessage = 'No hay datos disponibles',
  responsive = true,
  mobileCardRender
}: TableProps) {
  if (data.length === 0) {
    return (
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-8 text-center">
        <p className="text-gray-500">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className={`bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden ${className}`}>
      {/* Vista de tabla para pantallas grandes */}
      <div className={`${responsive ? 'hidden md:block' : ''} overflow-x-auto scrollbar-thin`}>
        <table className="min-w-full divide-y-2 divide-gray-200 bg-white text-sm">
          <thead className={headerClassName}>
            <tr>
              {columns.map((column) => (
                <th
                  key={column.key}
                  className={`whitespace-nowrap px-6 py-4 text-left font-semibold text-gray-900 ${column.className || ''}`}
                >
                  {column.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {data.map((row, index) => (
              <tr key={index} className={`hover:bg-gray-50 ${rowClassName}`}>
                {columns.map((column) => (
                  <td
                    key={column.key}
                    className={`px-6 py-4 font-medium text-gray-700 ${
                      column.key === 'note'
                        ? 'whitespace-normal max-w-[200px] break-words line-clamp-3'
                        : 'whitespace-nowrap'
                    }`}
                  >
                    {column.render ? column.render((row as Record<string, unknown>)[column.key], row) : String((row as Record<string, unknown>)[column.key] || '')}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Vista de tarjetas para pantallas pequeñas */}
      {responsive && (
        <div className="md:hidden">
          {mobileCardRender ? (
            <ul className="divide-y divide-gray-200">
              {data.map((item, index) => (
                <li key={index}>
                  {mobileCardRender(item)}
                </li>
              ))}
            </ul>
          ) : (
            <ul className="divide-y divide-gray-200">
              {data.map((row, index) => (
                <li key={index} className="p-4">
                  <div className="space-y-2">
                    {columns.map((column) => (
                      <div key={column.key} className="flex justify-between">
                        <span className="font-medium text-gray-600">{column.label}:</span>
                        <span className="font-semibold text-gray-900">
                          {column.render ? column.render((row as Record<string, unknown>)[column.key], row) : String((row as Record<string, unknown>)[column.key] || '')}
                        </span>
                      </div>
                    ))}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
});

export default Table;