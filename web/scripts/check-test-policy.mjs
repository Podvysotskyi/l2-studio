import { readdir, readFile } from 'node:fs/promises'
import { join, relative } from 'node:path'

const root = new URL('../test/', import.meta.url)
const failures = []

for (const file of await files(root)) {
  const contents = await readFile(file, 'utf8')
  const testPath = relative(root.pathname, file)
  if (testPath !== 'unit/storage.test.ts' &&
    /from ['"]node:fs|from ['"]node:fs\/promises|readFileSync?\(/.test(contents))
    failures.push(file)
}

if (failures.length) {
  throw new Error(
    `Web tests must use public module interfaces, not production source files. ` +
    `Only unit/storage.test.ts may access the filesystem:\n${failures.join('\n')}`
  )
}

async function files(directory) {
  const entries = await readdir(directory, { withFileTypes: true })
  const nested = await Promise.all(entries.map(async entry => {
    const path = join(directory.pathname, entry.name)
    return entry.isDirectory() ? files(new URL(`file://${path}/`)) : [path]
  }))
  return nested.flat().filter(file => file.endsWith('.test.ts'))
}
