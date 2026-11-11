import { expect, test } from "@playwright/test";

test.beforeEach(async ({ page, request }) => {
  const response = await request.get("/api/tasks");
  expect(response.ok()).toBeTruthy();
  const tasks: { id: number }[] = await response.json();
  for (const task of tasks) {
    const deleted = await request.delete(`/api/tasks/${task.id}`);
    expect(deleted.ok()).toBeTruthy();
  }

  await page.goto("/");
  await expect(page.locator("#loading")).toBeHidden();
});

test("creates and edits a task", async ({ page }) => {
  await page.getByLabel("What needs doing?").fill("Plan the week");
  await page.getByLabel("Notes (optional)").fill("Choose the top priorities");
  await page.getByLabel("Priority", { exact: true }).selectOption("High");
  await page.getByLabel("Due date", { exact: true }).fill("2028-02-01");
  await page.getByRole("button", { name: "+ Add task" }).click();

  const card = page.locator(".task-card");
  await expect(card).toHaveCount(1);
  await expect(card).toContainText("Plan the week");
  await expect(card).toContainText("Choose the top priorities");
  await expect(card.locator("time")).toHaveAttribute("datetime", "2028-02-01");

  await card.getByRole("button", { name: 'Edit "Plan the week"' }).click();
  await page.getByLabel("What needs doing?").fill("Plan next week");
  await page.getByRole("button", { name: "Save changes" }).click();

  await expect(page.locator(".task-card")).toContainText("Plan next week");
  await expect(page.locator(".task-card")).not.toContainText("Plan the week");
});

test("filters and searches tasks, then sorts by priority", async ({ page }) => {
  const entries = [
    { title: "Low priority", priority: "Low" },
    { title: "High priority", priority: "High" },
    { title: "Medium priority", priority: "Medium" },
  ];

  for (const [index, entry] of entries.entries()) {
    await page.getByLabel("What needs doing?").fill(entry.title);
    await page.getByLabel("Priority", { exact: true }).selectOption(entry.priority);
    await page.getByRole("button", { name: "+ Add task" }).click();
    await expect(page.locator(".task-card")).toHaveCount(index + 1);
  }

  await page.getByRole("checkbox", { name: 'Mark "Medium priority" complete' }).click();
  await page.getByRole("button", { name: "Completed" }).click();
  await expect(page.locator(".task-card")).toHaveCount(1);
  await expect(page.locator(".task-card")).toContainText("Medium priority");

  await page.getByRole("button", { name: "To do" }).click();
  await page.getByLabel("Search titles and notes").fill("high");
  await expect(page.locator(".task-card")).toHaveCount(1);
  await expect(page.locator(".task-card")).toContainText("High priority");

  await page.getByLabel("Search titles and notes").clear();
  await page.getByRole("button", { name: "All tasks" }).click();
  await page.getByLabel("Sort by").selectOption("priority");
  await expect(page.locator(".task-title")).toHaveText([
    "High priority",
    "Medium priority",
    "Low priority",
  ]);
});

test("completes a recurring task and confirms deleting its successor", async ({ page }) => {
  await page.getByLabel("What needs doing?").fill("Weekly review");
  await page.getByLabel("Repeat").selectOption("Weekly");
  await page.getByRole("button", { name: "+ Add task" }).click();

  await page.getByRole("checkbox", { name: 'Mark "Weekly review" complete' }).click();
  await expect(page.locator(".task-card")).toHaveCount(2);
  await expect(page.locator(".task-card.completed")).toHaveCount(1);
  const successor = page.locator(".task-card:not(.completed)");
  await expect(successor).toContainText("Repeats weekly");
  await expect(successor.locator("time")).toHaveAttribute("datetime", /^\d{4}-\d{2}-\d{2}$/);

  page.once("dialog", (dialog) => dialog.accept());
  await successor.getByRole("button", { name: 'Delete "Weekly review"' }).click();
  await expect(page.locator(".task-card")).toHaveCount(1);
  await expect(page.locator(".task-card.completed")).toHaveCount(1);
});

test("shows an API error and allows the task to be retried", async ({ page }) => {
  await page.route("**/api/tasks", (route) =>
    route.fulfill({
      status: 503,
      contentType: "application/problem+json",
      body: JSON.stringify({ detail: "Unable to save right now." }),
    }));

  await page.getByLabel("What needs doing?").fill("Retry this task");
  await page.getByRole("button", { name: "+ Add task" }).click();
  await expect(page.getByRole("alert")).toContainText("Unable to save right now.");

  await page.unroute("**/api/tasks");
  await page.getByRole("button", { name: "+ Add task" }).click();
  await expect(page.locator(".task-card")).toContainText("Retry this task");
});
