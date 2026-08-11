import puppeteer from 'puppeteer'
import http from 'node:http'
import net from 'node:net'

const chromePort = 9223
const proxyPort = 9222

const browser = await puppeteer.launch({
  headless: true,
  args: [
    '--remote-debugging-address=0.0.0.0',
    `--remote-debugging-port=${chromePort}`,
    '--remote-allow-origins=*',
    '--use-angle=swiftshader'
  ]
})

const proxy = http.createServer((request, response) => {
  const upstream = http.request(
    {
      hostname: '127.0.0.1',
      port: chromePort,
      path: request.url,
      method: request.method,
      headers: { ...request.headers, host: `127.0.0.1:${chromePort}` }
    },
    (upstreamResponse) => {
      response.writeHead(
        upstreamResponse.statusCode ?? 502,
        upstreamResponse.headers
      )
      upstreamResponse.pipe(response)
    }
  )
  upstream.on('error', () => response.destroy())
  request.pipe(upstream)
})

proxy.on('upgrade', (request, socket, head) => {
  const upstream = net.connect(chromePort, '127.0.0.1', () => {
    const headers = Object.entries(request.headers)
      .filter(([name]) => name.toLowerCase() !== 'host')
      .map(([name, value]) => `${name}: ${value}`)
    upstream.write(
      `${request.method} ${request.url} HTTP/${request.httpVersion}\r\n` +
        `host: 127.0.0.1:${chromePort}\r\n${headers.join('\r\n')}\r\n\r\n`
    )
    if (head.length > 0) upstream.write(head)
    socket.pipe(upstream).pipe(socket)
  })
  upstream.on('error', () => socket.destroy())
})

await new Promise((resolve, reject) => {
  proxy.once('error', reject)
  proxy.listen(proxyPort, '0.0.0.0', resolve)
})

process.stdout.write(
  `Chrome DevTools Protocol listening on port ${proxyPort}\n`
)

await new Promise((resolve) => {
  process.once('SIGINT', resolve)
  process.once('SIGTERM', resolve)
})

await new Promise((resolve) => proxy.close(resolve))
await browser.close()
