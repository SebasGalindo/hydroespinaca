# Dockerfile for frontend
FROM node:16
WORKDIR /app
COPY . .
RUN npm install && npm run build
CMD ["npm", "start"]
