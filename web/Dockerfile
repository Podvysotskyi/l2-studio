# syntax=docker/dockerfile:1.7
FROM node:24-bookworm-slim

WORKDIR /workspace
COPY . .
RUN --mount=type=secret,id=npm_token \
  sh -c 'NODE_AUTH_TOKEN="$(cat /run/secrets/npm_token)" npm install'

EXPOSE 3001
CMD ["npm", "run", "dev"]
