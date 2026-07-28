#!/usr/bin/env node
/**
 * Warn/fail if a packed playable exceeds the size budget.
 * Usage: node size-check.js <file> [--max 5242880] [--warn-only]
 */
const fs = require('fs');
const path = require('path');

function parseArgs(argv) {
  const out = { file: null, max: 5 * 1024 * 1024, warnOnly: false };
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--max') out.max = Number(argv[++i]);
    else if (a === '--warn-only') out.warnOnly = true;
    else if (!a.startsWith('-')) out.file = a;
  }
  return out;
}

function main() {
  const opts = parseArgs(process.argv);
  if (!opts.file) {
    console.error('Usage: node size-check.js <file> [--max bytes] [--warn-only]');
    process.exit(2);
  }
  const file = path.resolve(opts.file);
  if (!fs.existsSync(file)) {
    console.error('File not found:', file);
    process.exit(2);
  }
  const size = fs.statSync(file).size;
  const mb = (size / (1024 * 1024)).toFixed(2);
  const maxMb = (opts.max / (1024 * 1024)).toFixed(2);
  console.log(`${path.basename(file)}: ${size} bytes (${mb} MB), budget ${maxMb} MB`);
  if (size > opts.max) {
    const msg = `SIZE OVER BUDGET: ${mb} MB > ${maxMb} MB`;
    if (opts.warnOnly) {
      console.warn(msg);
      process.exit(0);
    }
    console.error(msg);
    process.exit(1);
  }
  console.log('OK within budget');
}

main();
