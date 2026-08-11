# syntax=docker/dockerfile:1.7
FROM node:24-bookworm-slim AS dependencies

WORKDIR /workspace
RUN chown node:node /workspace
COPY --chown=node:node package.json package-lock.json .npmrc ./
USER node
RUN --mount=type=cache,target=/home/node/.npm,uid=1000,gid=1000 \
    --mount=type=secret,id=npm_token,uid=1000 \
    NODE_AUTH_TOKEN="$(cat /run/secrets/npm_token)" \
    npm ci --include=dev --cache /home/node/.npm

FROM dependencies AS source
ARG APP_ENV=development
COPY --chown=node:node ".env.${APP_ENV}" .env
COPY --chown=node:node . .

FROM source AS validate
RUN npm test
RUN npm run typecheck
RUN npm run build

FROM source AS build
RUN npm run build

FROM source AS development
ENV NODE_ENV=development
EXPOSE 3001
CMD ["npm", "run", "dev"]

FROM node:24-bookworm-slim AS production
WORKDIR /workspace
ENV NODE_ENV=production
ENV PORT=3001
COPY --from=build --chown=node:node /workspace/.output ./.output
USER node
EXPOSE 3001
CMD ["node", ".output/server/index.mjs"]
