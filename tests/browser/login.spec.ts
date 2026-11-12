import { expect, test } from "@playwright/test";

test.use({ baseURL: "http://127.0.0.1:5201" });

async function signIn(page: import("@playwright/test").Page) {
  await page.getByLabel("Username", { exact: true }).fill("browser-user");
  await page.getByLabel("Password", { exact: true }).fill(process.env.DAYBOOK_TEST_PASSWORD!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page.locator("#workspace-content")).toBeVisible();
}

test("login gates tasks, persists on reload, and logout clears the workspace", async ({ page }) => {
  await page.goto("/");
  await expect(page.locator("#login-panel")).toBeVisible();
  await expect(page.locator("#workspace-content")).toBeHidden();
  await signIn(page);
  await page.getByLabel("What needs doing?").fill("Authenticated browser task");
  await page.getByRole("button", { name: "+ Add task" }).click();
  await expect(page.locator(".task-card").filter({ hasText: "Authenticated browser task" })).toHaveCount(1);
  await page.reload();
  await expect(page.locator("#workspace-content")).toBeVisible();
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
  await expect(page.locator("#login-panel")).toBeVisible();
  await expect(page.locator(".task-card")).toHaveCount(0);
  expect((await page.request.get("/api/tasks")).status()).toBe(401);
});

test("failed login shows an error and allows retry", async ({ page }) => {
  await page.goto("/");
  await page.getByLabel("Username", { exact: true }).fill("browser-user");
  await page.getByLabel("Password", { exact: true }).fill("incorrect");
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page.getByRole("alert")).toContainText("Invalid username or password.");
  await signIn(page);
  await expect(page.getByRole("alert")).toBeHidden();
});

test("expired authentication clears visible tasks and prompts for login", async ({ page, context }) => {
  await page.goto("/");
  await signIn(page);
  await context.clearCookies();
  await page.getByRole("button", { name: "Refresh", exact: true }).click();
  await expect(page.locator("#login-panel")).toBeVisible();
  await expect(page.locator(".task-card")).toHaveCount(0);
  await expect(page.getByRole("alert")).toContainText("sign in");
  await signIn(page);
});

test("signing out after expiry also clears the workspace", async ({ page, context }) => {
  await page.goto("/");
  await signIn(page);
  await context.clearCookies();
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
  await expect(page.locator("#login-panel")).toBeVisible();
  await expect(page.locator(".task-card")).toHaveCount(0);
});

test("session lookup failures fail closed and offer a retry", async ({ page }) => {
  await page.route("**/api/auth/session", route => route.fulfill({
    status: 503, contentType: "application/problem+json", body: JSON.stringify({ detail: "Authentication unavailable." }),
  }));
  await page.goto("/");
  await expect(page.locator("#workspace-content")).toBeHidden();
  await expect(page.getByRole("alert")).toContainText("Authentication unavailable.");
  await page.unroute("**/api/auth/session");
  await page.getByRole("button", { name: "Retry connection" }).click();
  await expect(page.locator("#login-panel")).toBeVisible();
});

test("pending login locks credentials and logout failure preserves the signed-in workspace", async ({ page }) => {
  let releaseLogin: () => void = () => { throw new Error("Login barrier not initialized."); };
  const pending = new Promise<void>(resolve => { releaseLogin = resolve; });
  await page.route("**/api/auth/login", async route => {
    await pending;
    await route.continue();
  });
  await page.goto("/");
  await page.getByLabel("Username", { exact: true }).fill("browser-user");
  await page.getByLabel("Password", { exact: true }).fill(process.env.DAYBOOK_TEST_PASSWORD!);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  try {
    await expect(page.getByLabel("Username", { exact: true })).toBeDisabled();
    await expect(page.getByLabel("Password", { exact: true })).toBeDisabled();
    await expect(page.getByRole("button", { name: "Sign in", exact: true })).toBeDisabled();
  } finally { releaseLogin(); }
  await expect(page.locator("#workspace-content")).toBeVisible();
  await page.route("**/api/auth/logout", route => route.fulfill({
    status: 503, contentType: "application/problem+json", body: JSON.stringify({ detail: "Sign out unavailable." }),
  }));
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
  await expect(page.getByRole("alert")).toContainText("Sign out unavailable.");
  await expect(page.locator("#workspace-content")).toBeVisible();
  await expect(page.getByRole("button", { name: "Sign out", exact: true })).toBeEnabled();
  await page.unroute("**/api/auth/logout");
  await page.getByRole("button", { name: "Sign out", exact: true }).click();
  await expect(page.locator("#login-panel")).toBeVisible();
});
