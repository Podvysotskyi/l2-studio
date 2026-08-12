import { readdir, readFile } from 'node:fs/promises'
import { extname, resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const browserSourceExtensions = new Set(['.js', '.jsx', '.ts', '.tsx', '.vue'])
const nativeDialogCall = /\b(?:window|globalThis|self)\s*\.\s*(?:alert|confirm|prompt)\s*\(/

describe('browser dialogs', () => {
  it('does not call browser-native dialogs from application source', async () => {
    const appRoot = resolve(import.meta.dirname, '../../app')
    const files = await sourceFiles(appRoot)
    const offenders: string[] = []

    for (const file of files) {
      const source = await readFile(file, 'utf8')
      if (nativeDialogCall.test(source)) {
        offenders.push(file.slice(appRoot.length + 1))
      }
    }

    expect(offenders).toEqual([])
  })
})

async function sourceFiles(directory: string): Promise<string[]> {
  const entries = await readdir(directory, { withFileTypes: true })
  const files = await Promise.all(entries.map(async (entry) => {
    const path = resolve(directory, entry.name)
    if (entry.isDirectory()) return sourceFiles(path)
    return browserSourceExtensions.has(extname(entry.name)) ? [path] : []
  }))
  return files.flat()
}
