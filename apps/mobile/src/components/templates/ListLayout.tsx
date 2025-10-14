import { View, Text, FlatList } from 'react-native';

export function ListLayout(): JSX.Element {
  return (
    <View style={{ flex: 1 }}>
      <Text style={{ padding: 16 }}>Soy un componente ListLayout</Text>
      <FlatList
        data={[]}
        renderItem={() => null}
        style={{ flex: 1 }}
      />
    </View>
  );
}