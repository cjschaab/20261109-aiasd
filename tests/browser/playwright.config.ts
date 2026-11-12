import { defineConfig } from "@playwright/test";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";
import { randomUUID } from "node:crypto";

const baseURL = "http://127.0.0.1:5200";
const databasePath = join(tmpdir(), `daybook-playwright-${process.pid}.db`);
const projectPath = resolve(import.meta.dirname, "../../src/TodoManager/TodoManager.csproj");
const loginURL = "http://127.0.0.1:5201";
const loginTaskPath = join(tmpdir(), `daybook-login-tasks-${process.pid}.db`);
const identityPath = join(tmpdir(), `daybook-login-identity-${process.pid}.db`);
process.env.DAYBOOK_TEST_PASSWORD ??= `Aa1!${randomUUID()}`;

export default defineConfig({
  testDir: ".",
  testMatch: "**/*.spec.ts",
  fullyParallel: false,
  workers: 1,
  reporter: "list",
  use: {
    baseURL,
    browserName: "chromium",
    headless: true,
  },
  webServer: [{
    command: `dotnet run --no-launch-profile --project "${projectPath}" -- --urls ${baseURL}`,
    url: baseURL,
    reuseExistingServer: false,
    timeout: 120_000,
    env: {
      ASPNETCORE_ENVIRONMENT: "Development",
      TaskDatabase__Path: databasePath,
      FeatureFlags__Login: "false",
    },
  }, {
    command: `dotnet run --no-launch-profile --no-build --project "${projectPath}" -- --urls ${loginURL}`,
    url: loginURL,
    reuseExistingServer: false,
    timeout: 120_000,
    env: {
      ASPNETCORE_ENVIRONMENT: "Development",
      TaskDatabase__Path: loginTaskPath,
      IdentityDatabase__Path: identityPath,
      IdentityBootstrap__UserName: "browser-user",
      IdentityBootstrap__Password: process.env.DAYBOOK_TEST_PASSWORD,
      FeatureFlags__Login: "true",
    },
  }],
});
