import React from 'react';
import BaseCard from './BaseCard';
import StatusBadge, { StatusType } from './StatusBadge';

export interface FuzzySystemCardProps {
  id: string;
  name: string;
  description: string;
  status: StatusType;
  defuzzificationMethod: string;
  operators: {
    and: string;
    or: string;
  };
  ruleNames: string[];
  variableNames: string[];
  routineNames: string[];
  lastUpdated?: string;
  onClick?: () => void;
}

const FuzzySystemCard: React.FC<FuzzySystemCardProps> = ({
  id,
  name,
  description,
  status,
  defuzzificationMethod,
  operators,
  ruleNames,
  variableNames,
  routineNames,
  lastUpdated,
  onClick
}) => {
  const handleCardClick = () => {
    if (onClick) {
      onClick();
    }
  };

  const formatLastUpdated = (dateString?: string) => {
    if (!dateString) return 'Sin actualizar';
    
    try {
      const date = new Date(dateString);
      return new Intl.RelativeTimeFormat('es', { numeric: 'auto' }).format(
        Math.ceil((date.getTime() - Date.now()) / (1000 * 60 * 60 * 24)),
        'day'
      );
    } catch {
      return 'Fecha inválida';
    }
  };

  return (
    <BaseCard 
      className="group transition-all duration-200 hover:shadow-md hover:border-green-300"
      onClick={handleCardClick}
      hover={true}
    >
      {/* Header */}
      <div className="flex items-start justify-between mb-4">
        <div className="flex-1">
          <h3 className="text-lg font-semibold text-gray-900 font-inter group-hover:text-green-700 transition-colors">
            {name}
          </h3>
          <p className="text-sm text-gray-600 font-inter mt-1">
            Defuzzificación: {defuzzificationMethod}
          </p>
          <p className="text-xs text-gray-500 font-inter mt-1">
            AND: {operators.and} | OR: {operators.or}
          </p>
        </div>
        <StatusBadge status={status} size="sm" />
      </div>

      {/* Contenido */}
      <div className="space-y-4">
        {/* Reglas */}
        <div className="p-3 bg-gray-50 rounded-lg">
          <div className="text-xs font-medium text-gray-700 font-inter mb-2">
            Reglas ({ruleNames.length})
          </div>
          <div className="text-sm text-gray-600 font-inter">
            {ruleNames.length > 0 ? (
              <div className="space-y-1">
                {ruleNames.slice(0, 2).map((rule, index) => (
                  <div key={index} className="truncate">• {rule}</div>
                ))}
                {ruleNames.length > 2 && (
                  <div className="text-xs text-gray-500">
                    +{ruleNames.length - 2} más
                  </div>
                )}
              </div>
            ) : (
              <div className="text-gray-400 italic">Sin reglas</div>
            )}
          </div>
        </div>
        
        {/* Variables */}
        <div className="p-3 bg-gray-50 rounded-lg">
          <div className="text-xs font-medium text-gray-700 font-inter mb-2">
            Variables ({variableNames.length})
          </div>
          <div className="text-sm text-gray-600 font-inter">
            {variableNames.length > 0 ? (
              <div className="space-y-1">
                {variableNames.slice(0, 2).map((variable, index) => (
                  <div key={index} className="truncate">• {variable}</div>
                ))}
                {variableNames.length > 2 && (
                  <div className="text-xs text-gray-500">
                    +{variableNames.length - 2} más
                  </div>
                )}
              </div>
            ) : (
              <div className="text-gray-400 italic">Sin variables</div>
            )}
          </div>
        </div>
        
        {/* Rutinas */}
        <div className="p-3 bg-gray-50 rounded-lg">
          <div className="text-xs font-medium text-gray-700 font-inter mb-2">
            Rutinas ({routineNames.length})
          </div>
          <div className="text-sm text-gray-600 font-inter">
            {routineNames.length > 0 ? (
              <div className="space-y-1">
                {routineNames.slice(0, 2).map((routine, index) => (
                  <div key={index} className="truncate">• {routine}</div>
                ))}
                {routineNames.length > 2 && (
                  <div className="text-xs text-gray-500">
                    +{routineNames.length - 2} más
                  </div>
                )}
              </div>
            ) : (
              <div className="text-gray-400 italic">Sin rutinas</div>
            )}
          </div>
        </div>
      </div>

      {/* Footer simplificado */}
      {lastUpdated && (
        <div className="pt-4 border-t border-gray-100">
          <div className="text-xs text-gray-500 font-inter">
            Actualizado {formatLastUpdated(lastUpdated)}
          </div>
        </div>
      )}
    </BaseCard>
  );
};

export default FuzzySystemCard;