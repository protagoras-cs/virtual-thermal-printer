import { parseArgs } from 'node:util'
import { join } from 'node:path'
import { readFile, writeFile } from 'node:fs/promises'
import { spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { dirname } from 'node:path'

const __dirname = dirname(fileURLToPath(import.meta.url))

const { values: flags } = parseArgs({
	args: process.argv.slice(2),
	options: {
		part: { type: 'string', default: 'patch' },
	},
})

const part = flags.part

if (part !== 'patch' && part !== 'minor' && part !== 'major') {
	console.error('Invalid version part. Use "patch", "minor", or "major".')
	process.exit(1)
}

const pkgPath = join(__dirname, '..', 'package.json')
const pkgJson = JSON.parse(await readFile(pkgPath, 'utf-8'))
const repoRoot = join(__dirname, '../../..')
const lockPath = join(repoRoot, 'package-lock.json')
const lockJson = JSON.parse(await readFile(lockPath, 'utf-8'))

const [major, minor, patch] = (pkgJson.version as string).split('.').map(Number)

let newVersion: string

switch (part) {
	case 'patch':
		newVersion = `${major}.${minor}.${patch + 1}`
		break
	case 'minor':
		newVersion = `${major}.${minor + 1}.0`
		break
	case 'major':
		newVersion = `${major + 1}.0.0`
		break
}

pkgJson.version = newVersion
await writeFile(pkgPath, JSON.stringify(pkgJson, null, '\t') + '\n')
if (lockJson.packages?.['packages/virtual-thermal-printer']) {
	lockJson.packages['packages/virtual-thermal-printer'].version = newVersion
}
await writeFile(lockPath, JSON.stringify(lockJson, null, '\t') + '\n')

console.log(`Version bumped to ${newVersion}`)

function runGit(args: string[]) {
	const result = spawnSync('git', args, { cwd: repoRoot, stdio: 'inherit' })
	if (result.status !== 0) {
		process.exit(result.status ?? 1)
	}
}

runGit(['add', '--', 'packages/virtual-thermal-printer/package.json', 'package-lock.json'])
runGit(['commit', '-m', `v${newVersion}`])
runGit(['tag', '-a', `v${newVersion}`, '-m', `Version ${newVersion}`])

console.log(`Created git tag v${newVersion}`)
