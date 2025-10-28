import React, { useMemo } from 'react';
import { View, ViewStyle, TouchableOpacity, Text } from 'react-native';
import { IconName } from '@hydroespinaca/shared';
import { Icon } from '../atoms/Icon';

export interface TabItem {
  key: string;
  title: string;
  icon?: IconName;
  badge?: number;
  disabled?: boolean;
  testID?: string;
}

export interface TabBarProps {
  tabs: TabItem[];
  activeTab: string;
  onTabPress: (tabKey: string) => void;
  variant?: 'default' | 'filled' | 'underline';
  size?: 'sm' | 'md' | 'lg';
  scrollable?: boolean;
  showBadges?: boolean;
  style?: ViewStyle;
  testID?: string;
}

export function TabBar({
  tabs,
  activeTab,
  onTabPress,
  variant = 'default',
  size = 'md',
  scrollable = false,
  showBadges = true,
  style,
  testID = 'tabbar',
}: TabBarProps): JSX.Element {
  const containerStyle = useMemo(() => ({
    flexDirection: 'row',
    backgroundColor: variant === 'filled' ? '#f3f4f6' : 'transparent',
    borderBottomWidth: variant === 'underline' ? 1 : 0,
    borderBottomColor: '#e5e7eb',
    paddingHorizontal: 16,
    ...(variant === 'filled' && {
      borderRadius: 12,
      margin: 8,
    }),
  }) as ViewStyle, [variant]);

  const getTabStyle = (isActive: boolean, disabled: boolean) => {
    const baseStyle = {
      flex: scrollable ? 0 : 1,
      paddingVertical: size === 'lg' ? 24 : size === 'sm' ? 8 : 16,
      paddingHorizontal: size === 'lg' ? 32 : size === 'sm' ? 16 : 24,
      alignItems: 'center',
      justifyContent: 'center',
      opacity: disabled ? 0.5 : 1,
      ...(variant === 'filled' && isActive && {
        backgroundColor: '#ffffff',
        borderRadius: 8,
        marginHorizontal: 4,
      }),
      ...(variant === 'underline' && isActive && {
        borderBottomWidth: 2,
        borderBottomColor: '#3b82f6',
      }),
    } as ViewStyle;

    return baseStyle;
  };

  const getTextColor = (isActive: boolean, disabled: boolean) => {
    if (disabled) return '#9ca3af';
    if (isActive) {
      return variant === 'filled' ? '#111827' : '#3b82f6';
    }
    return '#6b7280';
  };

  const renderBadge = (count: number) => {
    if (!showBadges || count <= 0) return null;
    
    return (
      <View
        style={{
          position: 'absolute',
          top: size === 'lg' ? 8 : 4,
          right: size === 'lg' ? 16 : 8,
          backgroundColor: '#ef4444',
          borderRadius: 9999,
          minWidth: 18,
          height: 18,
          paddingHorizontal: 4,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Text
          style={{
            fontSize: size === 'lg' ? 12 : 10,
            fontWeight: '500',
            color: '#ffffff',
          }}
        >
          {count > 99 ? '99+' : count.toString()}
        </Text>
      </View>
    );
  };

  return (
    <View style={[containerStyle, style]} testID={testID}>
      {tabs.map((tab) => {
        const isActive = tab.key === activeTab;
        const tabStyle = getTabStyle(isActive, tab.disabled || false);
        const textColor = getTextColor(isActive, tab.disabled || false);

        return (
          <TouchableOpacity
            key={tab.key}
            style={tabStyle}
            onPress={() => !tab.disabled && onTabPress(tab.key)}
            disabled={tab.disabled}
            testID={tab.testID || `${testID}-tab-${tab.key}`}
          >
            {tab.icon && (
              <Icon
                name={tab.icon}
                size={size === 'lg' ? 24 : size === 'sm' ? 16 : 20}
                color={textColor}
                style={{ marginBottom: 4 }}
              />
            )}
            <Text
              style={{
                fontSize: size === 'lg' ? 16 : size === 'sm' ? 12 : 14,
                color: textColor,
                fontWeight: isActive ? '500' : '400',
              }}
            >
              {tab.title}
            </Text>
            {tab.badge && renderBadge(tab.badge)}
          </TouchableOpacity>
        );
      })}
    </View>
  );
}