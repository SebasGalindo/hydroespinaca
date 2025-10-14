import { View, Text, ScrollView } from 'react-native';

export function DashboardLayout(): JSX.Element {
  return (
    <ScrollView style={{ flex: 1 }}>
      <View style={{ padding: 16 }}>
        <Text>Soy un componente DashboardLayout</Text>
      </View>
    </ScrollView>
  );
}