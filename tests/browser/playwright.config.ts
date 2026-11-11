import { defineConfig } from "@playwright/test";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";

const baseURL = "http://127.0.0.1:5200";
const databasePath = join(tmpdir(), `daybook-playwright-${process.pid}.db`);
const projectPath = resolve(import.meta.dirname, "../../src/TodoManager/TodoManager.csproj");

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
  webServer: {
    command: `dotnet run --no-launch-profile --project "${projectPath}" -- --urls ${baseURL}`,
    url: baseURL,
    reuseExistingServer: false,
    timeout: 120_000,
    env: {
      ASPNETCORE_ENVIRONMENT: "Development",
      TaskDatabase__Path: databasePath,
    },
  },
});
