import React from 'react';
import { SensorIcon, TrendingUpIcon, BrainIcon, PlusIcon } from '@/components/ui/icons/Icons';

interface ActionCardProps {
  iconType: 'sensor' | 'chart' | 'trending' | 'brain' | 'plus';
  title: string;
  description: string;
  buttonText: string;
  buttonColor: 'green' | 'blue' | 'gray';
}

const ActionCard: React.FC<ActionCardProps> = ({
  iconType,
  title,
  description,
  buttonText,
  buttonColor
}) => {
  const getIcon = () => {
    const iconProps = { size: 24, color: '#16a34a' };
    switch (iconType) {
      case 'sensor':
        return <SensorIcon {...iconProps} />;
      case 'chart':
      case 'trending':
        return <TrendingUpIcon {...iconProps} />;
      case 'brain':
        return <BrainIcon {...iconProps} />;
      case 'plus':
        return <PlusIcon {...iconProps} />;
      default:
        return null;
    }
  };
  
  const getButtonClass = () => {
    switch (buttonColor) {
      case 'green':
        return 'hidro-button-primary';
      case 'blue':
        return 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500 hidro-button';
      case 'gray':
        return 'hidro-button-secondary';
      default:
        return 'hidro-button-primary';
    }
  };

  return (
    <article className="hidro-card p-6">
      <header className="flex items-center mb-4">
        <div className="flex-shrink-0 mr-4">
          {getIcon()}
        </div>
        <h3 className="text-lg font-semibold text-gray-900 font-inter">{title}</h3>
      </header>
      
      <div className="mb-6">
        <p className="text-gray-600 text-sm leading-relaxed font-inter">
          {description}
        </p>
      </div>
      
      <footer>
        <button className={`${getButtonClass()} w-full font-inter`}>
          {buttonText}
        </button>
      </footer>
    </article>
  );
};

export default ActionCard;