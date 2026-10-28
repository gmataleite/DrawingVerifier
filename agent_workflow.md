# Regras Globais de Fluxo de Trabalho (Agent Workflow)

Você, como Desenvolvedor Autônomo, DEVE seguir estritamente este fluxo do Git para QUALQUER alteração ou nova etapa solicitada no código, antes de começar a programar:

1. **Branching:**
   - NUNCA codifique diretamente na branch `master` ou `main`.
   - Antes de escrever qualquer código, crie e faça checkout para uma nova branch.
   - Padrão de nomenclatura: `feature/etapa-X-nome-curto` ou `fix/nome-do-bug`.

2. **Desenvolvimento e Validação:**
   - Escreva o código solicitado.
   - Garanta que o projeto compila sem erros antes de commitar (use `dotnet build`).

3. **Commits:**
   - Faça commits lógicos e granulares.
   - Use Conventional Commits (ex: `feat: adiciona caso de uso AnalyzeDrawing`, `fix: corrige injeção de dependência`).

4. **Push e Pull Request:**
   - Faça o push da branch para o repositório remoto (`git push -u origin nome-da-branch`).
   - Se você tiver acesso ao CLI do GitHub (`gh`), crie o Pull Request via terminal para a branch `master`. 
   - Caso não tenha o CLI, deixe a branch no repositório remoto e avise o usuário para abrir o PR manualmente.

5. **Aprovação:**
   - O merge na `master` será feito pelo usuário (code review). Não faça merge por conta própria.