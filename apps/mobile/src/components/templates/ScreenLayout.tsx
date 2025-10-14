import { View, SafeAreaView } from 'react-native';
import React from 'react';
import { colors } from '@hidroespinaca/shared';

type ScreenLayoutProps = {
  children: React.ReactNode;
};

export function ScreenLayout({ children }: ScreenLayoutProps): React.ReactElement {
  return (
    <SafeAreaView style={{ flex: 1, backgroundColor: colors.hidro.bgLight }}>
      <View style={{ flex: 1 }}>
        {children}
      </View>
    </SafeAreaView>
  );
}