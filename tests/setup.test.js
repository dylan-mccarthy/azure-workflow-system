// Simple test to verify Jest setup
describe('CI/CD Setup', () => {
  test('should pass basic test', () => {
    expect(true).toBe(true);
  });

  test('should verify package.json exists', () => {
    const fs = require('fs');
    const path = require('path');

    const packagePath = path.join(process.cwd(), 'package.json');
    expect(fs.existsSync(packagePath)).toBe(true);
  });
});
