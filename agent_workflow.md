# Global Workflow Rules (Agent Workflow)

As an Autonomous Developer, you MUST strictly follow this Git workflow for ANY code changes or new features requested, before writing any code:

1. **Branching:**
   - NEVER code directly on the `main` or `master` branch.
   - Before writing code, create and checkout a new branch.
   - Naming convention: `feature/phase-X-short-name` or `fix/bug-name`.

2. **Development & Validation:**
   - Write the requested code.
   - Ensure the project compiles without errors before committing (run `dotnet build`).

3. **Commits:**
   - Make logical, granular commits.
   - Use Conventional Commits (e.g., `feat: add AnalyzeDrawing use case`, `fix: resolve dependency injection error`).

4. **Push & Pull Request:**
   - Push the branch to the remote repository (`git push -u origin <branch-name>`).
   - If you have access to the GitHub CLI (`gh`), create a Pull Request to the `main` branch via terminal.
   - If the CLI is not available, leave the branch in the remote repository and notify the user to open the PR manually.

5. **Approval:**
   - Merging into `main` will be done by the user (code review). Do not merge on your own.
