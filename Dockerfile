FROM node:24-bookworm-slim

WORKDIR /workspace
COPY . .

EXPOSE 3001
CMD ["sh", "-c", "npm ci && npm run dev"]
