module.exports = function (api) {
  api.cache(true);
  return {
    presets: [
      [
        'babel-preset-expo',
        {
          // Enable import.meta transformation for React Native/Hermes
          unstable_transformImportMeta: true,
        },
      ],
    ],
    plugins: [],
  };
};
