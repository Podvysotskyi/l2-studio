# syntax=docker/dockerfile:1.7
FROM node:24-bookworm-slim AS dependencies

WORKDIR /workspace
RUN chown node:node /workspace
COPY --chown=node:node package.json package-lock.json ./
USER node
RUN --mount=type=cache,target=/home/node/.npm,uid=1000,gid=1000 \
    npm ci --include=dev --cache /home/node/.npm

FROM dependencies AS source
ARG APP_ENV=development
COPY --chown=node:node ".env.${APP_ENV}" .env
COPY --chown=node:node . .
RUN mkdir /workspace/resources && npm run postinstall

FROM source AS validate
RUN npm test
RUN npm run typecheck
RUN npm run build

FROM mcr.microsoft.com/playwright:v1.62.1-noble AS browser-tests

WORKDIR /workspace
COPY --from=source --chown=pwuser:pwuser /workspace /workspace
RUN chown -R pwuser:pwuser /workspace
USER pwuser
RUN npm run test:e2e

FROM source AS coverage
RUN npm run test:coverage

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
