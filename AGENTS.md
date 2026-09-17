# AGENTS.md — Volleyball 3D

## Projeto

Nome provisório: Volleyball 3D

Engine: Unity 6  
Linguagem: C#  
Tipo: Jogo esportivo 3D de vôlei  
Plataforma inicial: Windows PC

Este projeto será desenvolvido de forma incremental com auxílio do Codex.

A prioridade é:

1. Gameplay funcional
2. Código simples e modular
3. Facilidade de manutenção
4. Bom desempenho
5. Multiplayer posteriormente
6. Qualidade visual posteriormente

---

# REGRA PRINCIPAL

Implemente somente o que foi solicitado na tarefa atual.

Não antecipe funcionalidades futuras.

Não faça melhorias, refatorações ou alterações extras sem necessidade direta.

---

# ECONOMIA DE CONTEXTO

Para reduzir processamento e uso desnecessário:

- Leia somente os arquivos necessários para a tarefa.
- Não faça análise completa do projeto sem solicitação.
- Não percorra todas as pastas por padrão.
- Não abra assets gráficos quando a tarefa for apenas de código.
- Não examine arquivos de terceiros sem necessidade.
- Ignore pastas geradas automaticamente pelo Unity.
- Não leia arquivos binários.
- Não analise o histórico Git inteiro sem solicitação.

Pastas normalmente irrelevantes:

Library/
Temp/
Logs/
obj/
Build/
Builds/
UserSettings/

Não altere essas pastas.

---

# ESCOPO DAS TAREFAS

Antes de modificar código:

1. Identifique o objetivo.
2. Identifique os arquivos diretamente relacionados.
3. Leia somente esses arquivos e suas dependências imediatas.
4. Faça a menor alteração capaz de resolver a tarefa.
5. Valide.
6. Pare.

Não continue procurando melhorias depois que o objetivo estiver concluído.

---

# ALTERAÇÕES NÃO SOLICITADAS

Não faça automaticamente:

- refatoração;
- renomeação;
- reorganização de pastas;
- troca de arquitetura;
- otimização;
- alteração visual;
- instalação de pacote;
- atualização de dependência;
- alteração de configuração do projeto;
- mudança de Input System;
- mudança de pipeline gráfico;
- alteração em cenas não relacionadas;
- criação de sistemas futuros.

Caso perceba uma possível melhoria fora do escopo:

Informe resumidamente no relatório final.

Não implemente.

---

# DEPENDÊNCIAS

Não instalar pacotes sem solicitação explícita.

Antes de adicionar uma dependência:

1. Verifique se Unity ou C# já oferecem solução adequada.
2. Prefira solução nativa quando razoável.
3. Se um pacote externo for realmente necessário, explique antes.

---

# ORGANIZAÇÃO DE CÓDIGO

Scripts próprios devem ficar em:

Assets/Scripts/

Estrutura principal:

Assets/Scripts/
├── Core/
├── Player/
├── Ball/
├── Gameplay/
├── Match/
├── AI/
├── Camera/
├── UI/
├── Audio/
└── Networking/

Não criar novas pastas sem necessidade.

---

# RESPONSABILIDADES

Cada script deve possuir uma responsabilidade principal.

Exemplos:

PlayerMovement.cs
Responsável por movimentação.

PlayerJump.cs
Responsável por salto.

VolleyballBall.cs
Responsável pelo estado básico da bola.

BallPhysics.cs
Responsável por forças, velocidade e rotação.

ServeSystem.cs
Responsável por saque.

ReceiveSystem.cs
Responsável por recepção.

SetSystem.cs
Responsável por levantamento.

SpikeSystem.cs
Responsável por ataque.

BlockSystem.cs
Responsável por bloqueio.

MatchManager.cs
Responsável pelo estado da partida.

ScoreManager.cs
Responsável pela pontuação.

---

# TAMANHO DOS SCRIPTS

Evite scripts gigantes.

Não divida código apenas para reduzir número de linhas.

Separe quando houver responsabilidades claramente diferentes.

Prefira código simples e legível.

---

# PADRÃO DE C#

Utilizar:

- PascalCase para classes, métodos e propriedades públicas.
- camelCase para variáveis locais.
- _camelCase para campos privados.
- [SerializeField] para referências configuráveis pelo Inspector.

Evitar campos públicos apenas para exposição no Inspector.

Exemplo:

[SerializeField] private float _moveSpeed = 6f;

---

# GAMEPLAY PRIMEIRO

Durante a fase inicial:

Priorizar comportamento correto sobre aparência.

Usar:

- cápsulas;
- cubos;
- materiais simples;
- modelos provisórios;
- animações provisórias.

Não investir tempo em gráficos avançados até o gameplay principal estar validado.

---

# FÍSICA

O jogo deve possuir comportamento consistente e previsível.

Evite números mágicos espalhados pelo código.

Valores ajustáveis devem preferencialmente aparecer no Inspector.

Exemplos:

- força do saque;
- força da cortada;
- velocidade máxima da bola;
- força de levantamento;
- altura do salto;
- velocidade do jogador.

---

# BOLA

A bola é um elemento central do jogo.

Alterações relacionadas à bola devem evitar quebrar:

- gravidade;
- velocidade;
- direção;
- colisão;
- rotação;
- detecção de quadra;
- contato com jogadores.

Não implementar sistemas avançados de spin antes da física básica funcionar corretamente.

---

# JOGADOR

O jogador deve possuir sistemas independentes para:

- movimentação;
- salto;
- ações de vôlei;
- animação;
- estado;
- atributos.

Não colocar toda a lógica dentro de PlayerController.

PlayerController pode coordenar sistemas, mas não deve concentrar todas as responsabilidades.

---

# INPUT

Na fase inicial, controles previstos:

WASD = movimentação

Shift = corrida

Space = salto

Botões das ações de vôlei serão definidos posteriormente.

Não alterar o sistema de input global sem necessidade.

---

# INTELIGÊNCIA ARTIFICIAL

IA será desenvolvida em etapas.

Ordem:

1. Movimentação básica
2. Identificação da trajetória da bola
3. Posicionamento
4. Recepção
5. Levantamento
6. Ataque
7. Formação
8. Decisão tática

Não criar IA complexa antes dos sistemas básicos de gameplay estarem funcionando.

---

# MULTIPLAYER

Multiplayer será implementado somente depois do gameplay single-player estar estável.

Porém:

Evite arquitetura que dependa desnecessariamente de Singleton global ou referências impossíveis de separar posteriormente.

Não implementar networking antecipadamente.

---

# TESTES

Execute somente validações relacionadas às alterações realizadas.

Evite builds completos repetidos quando uma compilação menor ou teste específico for suficiente.

Ao corrigir erro:

Corrija a causa.

Não esconda erros ou warnings relevantes.

---

# ERROS EXISTENTES

Caso encontre erro claramente não relacionado à tarefa:

Não corrija automaticamente.

Informe:

"Problema existente detectado fora do escopo."

Continue somente se o erro impedir a tarefa atual.

---

# CENAS

Durante o protótipo:

Assets/Scenes/

Cena principal inicial:

PrototypeCourt.unity

Evite criar várias cenas desnecessariamente.

---

# PREFABS

Elementos reutilizáveis devem eventualmente virar Prefabs.

Exemplos:

Player
Volleyball
Net
Court

Não converter tudo para Prefab prematuramente.

---

# DOCUMENTAÇÃO

Comentários devem explicar decisões não óbvias.

Não comentar código trivial.

Evitar documentação longa dentro dos scripts.

---

# GIT

Faça alterações pequenas e relacionadas.

Não altere dezenas de arquivos para uma tarefa simples.

Nunca:

- delete arquivos sem necessidade;
- sobrescreva assets importantes;
- altere configurações globais sem solicitação.

---

# FORMATO DA RESPOSTA DO CODEX

Depois de completar uma tarefa, responder de forma curta.

Formato:

## Concluído

Arquivos alterados:
- arquivo1
- arquivo2

Implementado:
- item
- item

Validação:
- resultado

Pendências relevantes:
- somente se houver

Não escrever explicações longas se a implementação estiver concluída.

---

# QUANDO PARAR

Assim que:

- o comportamento solicitado existir;
- o código compilar;
- a validação necessária passar;

pare.

Não procure novas tarefas.

---

# DOCUMENTO DE DESIGN

Antes de tomar decisões importantes sobre gameplay, consultar:

GAME_DESIGN.md

Não alterar o GAME_DESIGN.md automaticamente.

Somente modificá-lo mediante solicitação explícita.