'use client';

import React from 'react';
import dynamic from 'next/dynamic';
import { SimpleFuzzyVariable, SimpleFuzzyTerm } from '@hydroespinaca/shared';

// Importación dinámica de Plotly para evitar problemas de SSR
const Plot = dynamic(() => import('react-plotly.js'), { ssr: false });

interface MembershipChartProps {
  variable: SimpleFuzzyVariable;
  terms: SimpleFuzzyTerm[];
}

const MembershipChart: React.FC<MembershipChartProps> = ({ variable, terms }) => {
  // Colores para diferenciar cada término
  const colors = [
    '#3B82F6', // blue-500
    '#EF4444', // red-500
    '#10B981', // emerald-500
    '#F59E0B', // amber-500
    '#8B5CF6', // violet-500
    '#EC4899', // pink-500
    '#06B6D4', // cyan-500
    '#84CC16', // lime-500
  ];

  // Función para calcular la función de membresía triangular
  const calculateTriangular = (x: number, a: number, b: number, c: number): number => {
    if (x <= a || x >= c) return 0;
    if (x === b) return 1;
    if (x < b) return (x - a) / (b - a);
    return (c - x) / (c - b);
  };

  // Función para calcular la función de membresía trapezoidal
  const calculateTrapezoidal = (x: number, a: number, b: number, c: number, d: number): number => {
    if (x <= a || x >= d) return 0;
    if (x >= b && x <= c) return 1;
    if (x < b) return (x - a) / (b - a);
    return (d - x) / (d - c);
  };

  // Función para calcular la función de membresía gaussiana
  const calculateGaussian = (x: number, mean: number, sigma: number): number => {
    return Math.exp(-0.5 * Math.pow((x - mean) / sigma, 2));
  };

  // Generar puntos para la gráfica
  const generateMembershipData = () => {
    // Usar un rango por defecto si no está disponible en la variable
    const minValue = terms.length > 0 ? (terms[0]?.membership_function?.universe_min ?? 0) : 0;
    const maxValue = terms.length > 0 ? (terms[0]?.membership_function?.universe_max ?? 100) : 100;
    const step = (maxValue - minValue) / 200;
    const xValues: number[] = [];
    
    // Generar valores de x
    for (let x = minValue; x <= maxValue; x += step) {
      xValues.push(x);
    }

    return terms.map((term, index) => {
      const yValues = xValues.map(x => {
        const params = term.membership_function.parameters;
        
        switch (term.membership_function.function_type) {
          case 'triangular':
            return calculateTriangular(x, params[0] ?? 0, params[1] ?? 0, params[2] ?? 0);
          case 'trapezoidal':
            return calculateTrapezoidal(x, params[0] ?? 0, params[1] ?? 0, params[2] ?? 0, params[3] ?? 0);
          case 'gaussian':
            return calculateGaussian(x, params[0] ?? 0, params[1] ?? 1);
          default:
            return 0;
        }
      });

      return {
        x: xValues,
        y: yValues,
        type: 'scatter' as const,
        mode: 'lines' as const,
        name: term.label,
        line: {
          color: colors[index % colors.length] || '#3B82F6',
          width: 2
        },
        fill: 'tonexty' as const,
        fillcolor: colors[index % colors.length] + '20', // 20% opacity
      };
    });
  };

  const data = generateMembershipData();

  const layout = {
    title: {
      text: `Funciones de Membresía - ${variable.name}`,
      font: { size: 16, family: 'Inter, sans-serif' }
    },
    xaxis: {
      title: {
        text: variable.name,
        font: { family: 'Inter, sans-serif' }
      },
      range: terms.length > 0 ? [terms[0]?.membership_function?.universe_min || 0, terms[0]?.membership_function?.universe_max || 100] : [0, 100],
      gridcolor: '#E5E7EB',
      font: { family: 'Inter, sans-serif' }
    },
    yaxis: {
      title: {
        text: 'Grado de Membresía',
        font: { family: 'Inter, sans-serif' }
      },
      range: [0, 1.1],
      gridcolor: '#E5E7EB',
      font: { family: 'Inter, sans-serif' }
    },
    plot_bgcolor: '#FFFFFF',
    paper_bgcolor: '#FFFFFF',
    font: { family: 'Inter, sans-serif' },
    legend: {
      orientation: 'h' as const,
      x: 0,
      y: -0.2,
      font: { family: 'Inter, sans-serif' }
    },
    margin: {
      l: 60,
      r: 40,
      t: 60,
      b: 80
    },
    hovermode: 'x unified' as const,
  };

  const config = {
    displayModeBar: true,
    displaylogo: false,
    modeBarButtonsToRemove: [
      'pan2d',
      'lasso2d',
      'select2d',
      'autoScale2d',
      'hoverClosestCartesian',
      'hoverCompareCartesian',
      'toggleSpikelines'
    ] as any,
    responsive: true,
  };

  return (
    <div className="bg-white border border-gray-200 rounded-lg p-4">
      <Plot
        data={data}
        layout={layout}
        config={config}
        style={{ width: '100%', height: '400px' }}
      />
    </div>
  );
};

export default MembershipChart;