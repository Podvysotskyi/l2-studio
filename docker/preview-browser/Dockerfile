FROM ghcr.io/puppeteer/puppeteer:25.0.4

COPY --chown=pptruser:pptruser docker/preview-browser/server.mjs /home/pptruser/server.mjs

CMD ["node", "/home/pptruser/server.mjs"]
