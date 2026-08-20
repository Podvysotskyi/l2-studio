import { readFile } from 'node:fs/promises'

const baseline = JSON.parse(await readFile(new URL('../coverage-baseline.json', import.meta.url), 'utf8'))
const projects = ['unit', 'nuxt']
const metrics = ['lines', 'statements', 'functions', 'branches']
const failures = []

for (const project of projects) {
  const summary = JSON.parse(await readFile(
    new URL(`../coverage/${project}/coverage-summary.json`, import.meta.url),
    'utf8'
  ))
  for (const metric of metrics) {
    const actual = summary.total[metric].pct
    const minimum = baseline[project][metric]
    if (actual < minimum)
      failures.push(`${project} ${metric}: ${actual}% is below the ${minimum}% baseline`)
  }
}

if (failures.length)
  throw new Error(`Coverage regressed:\n${failures.join('\n')}`)
