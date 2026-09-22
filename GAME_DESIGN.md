# GAME_DESIGN.md — Volleyball 3D

## VISÃO

Volleyball 3D será um jogo esportivo em terceira pessoa focado em partidas de vôlei com controles acessíveis, movimentação fluida e profundidade tática.

Objetivo inicial:

Criar primeiro um protótipo divertido e funcional.

Realismo visual não é prioridade no início.

---

# PLATAFORMA INICIAL

PC — Windows

Possíveis plataformas futuras:

- Steam
- consoles
- eventualmente mobile, caso a arquitetura permita

Não desenvolver sistemas específicos dessas plataformas durante o protótipo.

---

# ENGINE

Unity 6

Linguagem:

C#

---

# PERSPECTIVA

Terceira pessoa.

A câmera acompanha o jogador controlado.

A câmera deve permitir leitura fácil de:

- bola;
- rede;
- companheiros;
- adversários;
- espaço livre da quadra.

---

# PRIMEIRO PROTÓTIPO

O primeiro objetivo jogável será:

1 jogador contra 1 jogador controlado por IA.

Quadra reduzida ou convencional.

Sistemas necessários:

- jogador;
- movimentação;
- salto;
- bola;
- rede;
- colisão;
- saque;
- toque simples;
- pontuação.

Esse protótipo serve para validar a base.

---

# META FINAL DE GAMEPLAY

O jogo deverá posteriormente suportar:

6 x 6

com as posições tradicionais:

- Levantador
- Oposto
- Ponteiro
- Central
- Líbero

---

# LOOP PRINCIPAL

Fluxo normal de uma jogada:

Saque
↓
Recepção
↓
Levantamento
↓
Ataque
↓
Bloqueio / Defesa
↓
Contra-ataque

O gameplay deve tornar esse ciclo satisfatório.

---

# MOVIMENTAÇÃO

Jogador deve conseguir:

- andar;
- correr;
- mudar direção;
- saltar;
- posteriormente mergulhar;
- posteriormente executar deslocamentos específicos de bloqueio.

Movimentação precisa responder rapidamente ao controle.

---

# CONTROLES INICIAIS

WASD
Movimentação

Shift
Corrida

Space
Salto

Os comandos das ações de vôlei serão definidos após os testes iniciais.

---

# SISTEMA DE BOLA

A bola deverá possuir:

- gravidade;
- velocidade;
- direção;
- colisão;
- impulso;
- limite de velocidade;
- posteriormente rotação/spin.

A trajetória precisa ser suficientemente previsível para permitir habilidade do jogador.

Não buscar simulação física perfeita.

Prioridade:

Física divertida e consistente.

---

# CONTATO COM A BOLA

Ações não devem depender exclusivamente de colisão física direta da mão.

Utilizar sistema controlado de zonas de contato quando necessário.

Isso permitirá controlar melhor:

- manchetes;
- levantamentos;
- cortadas;
- bloqueios;
- saques.

---

# RECEPÇÃO

Objetivo:

Transformar uma bola adversária em passe direcionado para uma área escolhida.

Variáveis futuras:

- qualidade da recepção;
- posicionamento;
- velocidade da bola;
- atributo do jogador;
- timing.

---

# LEVANTAMENTO

O levantamento deverá permitir direcionar a bola para diferentes opções de ataque.

Futuramente:

- ponta;
- meio;
- saída;
- fundo;
- bolas rápidas;
- bolas altas.

---

# CORTADA

A cortada deve ser uma das ações mais satisfatórias do jogo.

Elementos:

- aproximação;
- salto;
- timing;
- direção;
- força;
- ponto de contato.

Posteriormente adicionar:

- diferentes tipos de ataque;
- diagonal;
- paralela;
- largada;
- explorada no bloqueio.

---

# BLOQUEIO

Bloqueio inicialmente dependerá de:

- posição;
- salto;
- área de contato.

Posteriormente considerar:

- direção das mãos;
- timing;
- leitura do levantador;
- bloqueio duplo;
- bloqueio triplo.

---

# SAQUE

Primeiros tipos:

Saque simples
Saque por cima

Posteriormente:

- saque flutuante;
- saque viagem;
- controle de força;
- controle de direção.

---

# ATRIBUTOS DOS JOGADORES

Sistema futuro:

Ataque
Bloqueio
Recepção
Defesa
Levantamento
Saque
Velocidade
Salto

Escala sugerida:

0–100

Exemplo:

Ataque: 84
Bloqueio: 71
Recepção: 92
Defesa: 88
Levantamento: 60
Saque: 79
Velocidade: 86
Salto: 89

Não implementar atributos antes do gameplay básico.

---

# IA

Objetivo futuro:

Fazer a equipe se comportar como uma equipe de vôlei e não como vários personagens correndo atrás da bola.

Cada jogador deve possuir:

posição base
+
responsabilidade atual
+
leitura da jogada

Fluxo conceitual:

Bola
↓
Trajetória prevista
↓
Quem é responsável?
↓
Jogador se posiciona
↓
Executa ação

---

# IA — FASE 1

IA básica para protótipo 1x1:

- localizar bola;
- prever ponto aproximado de queda;
- movimentar até a região;
- devolver bola.

---

# IA — FASE 2

Para equipes:

Definir responsabilidades.

Exemplo:

Recebedor
↓
Levantador
↓
Atacante

Nem todos devem perseguir a bola.

---

# IA — FASE 3

Decisão tática:

- escolher atacante;
- escolher região do saque;
- direcionar ataques;
- organizar bloqueio;
- cobrir companheiros;
- alterar formação.

---

# AI DIFFICULTY SYSTEM — FUTURO

O sistema de dificuldade da IA será implementado somente após a estabilização
das regras e do gameplay offline.

Possíveis variáveis futuras:

- reactionDelay;
- predictionAccuracy;
- positioningError;
- receiveAccuracy;
- setAccuracy;
- spikeAccuracy;
- spikeTargetQuality;
- decisionAggressiveness;
- recoverySpeed;
- mistakeChance.

Nenhum valor ou comportamento de dificuldade é aplicado nesta fase.

---

# CÂMERA

Primeiro modelo:

Terceira pessoa.

A câmera deve:

- acompanhar jogador;
- manter a bola visível sempre que possível;
- evitar movimentos bruscos;
- facilitar percepção de profundidade.

Câmeras especiais poderão ser adicionadas posteriormente para:

- saque;
- cortada;
- replay;
- comemoração.

---

# PONTUAÇÃO

Sistema final baseado em vôlei convencional.

Para protótipo:

- ponto quando bola toca o chão;
- ponto quando bola sai;
- reinício da jogada;
- placar.

Regras avançadas serão adicionadas posteriormente.

---

# PARTIDAS

Estrutura futura:

Ponto
↓
Set
↓
Partida

Posteriormente:

- melhor de 3;
- melhor de 5;
- regras configuráveis.

---

# ANIMAÇÕES

Durante protótipo:

Animações simples ou provisórias.

Posteriormente:

Idle
Corrida
Salto
Manchete
Levantamento
Ataque
Bloqueio
Saque
Mergulho
Comemoração

A animação deve apoiar o gameplay e não controlar toda a lógica.

---

# PERSONAGENS

Inicialmente:

personagens genéricos.

Posteriormente:

- aparência;
- altura;
- peso;
- cabelo;
- uniforme;
- número;
- posição;
- atributos.

---

# EQUIPES

Sistema futuro permitirá:

- nome;
- uniforme;
- elenco;
- formação;
- escalação.

---

# MODOS FUTUROS

Depois do gameplay principal:

Partida rápida

Campeonato

Treino

Modo carreira

Criação de jogador

Multiplayer online

Possível modo 2x2

Possível modo 4x4

Possível vôlei de praia

Nenhum desses modos deve ser desenvolvido durante o primeiro protótipo.

---

# MULTIPLAYER

Multiplayer é objetivo futuro importante.

Por isso, lógica crítica deve evitar dependência desnecessária de elementos exclusivamente locais.

Entretanto:

Não implementar networking durante a fase inicial.

Primeiro estabilizar gameplay offline.

---

# GRÁFICOS

Primeira etapa:

Visual simples.

Prioridades:

1. Jogabilidade
2. Legibilidade
3. Física
4. IA
5. Animações
6. Interface
7. Gráficos avançados

---

# ÁUDIO

Posteriormente incluir:

- contato com bola;
- tênis na quadra;
- apito;
- torcida;
- rede;
- impacto de cortada;
- interface.

---

# ROADMAP

## FASE 0 — Projeto

Criar projeto Unity.

Criar estrutura de pastas.

Adicionar AGENTS.md.

Adicionar GAME_DESIGN.md.

Criar Git.

---

## FASE 1 — Quadra

Criar:

- piso;
- limites;
- rede;
- bola;
- jogador provisório;
- câmera.

Sem gameplay complexo.

---

## FASE 2 — Movimento

Implementar:

- movimentação;
- corrida;
- rotação;
- salto.

Meta:

Jogador deve ser agradável de controlar.

---

## FASE 3 — Bola

Implementar:

- Rigidbody;
- gravidade;
- colisões;
- impulso;
- limite de velocidade.

Meta:

Bola deve possuir trajetória consistente.

---

## FASE 4 — Interação

Implementar:

- zona de contato;
- toque básico;
- direção da bola.

Meta:

Jogador consegue trocar a bola sobre a rede.

---

## FASE 5 — Vôlei básico

Adicionar:

- saque;
- recepção;
- levantamento;
- ataque;
- bloqueio.

Um sistema por vez.

---

## FASE 6 — Regras

Adicionar:

- dentro/fora;
- chão;
- rede;
- pontuação;
- reinício;
- sets.

---

## FASE 7 — IA 1x1

Adicionar adversário capaz de:

- ler bola;
- posicionar;
- devolver;
- sacar.

---

## FASE 8 — POLIMENTO

Ajustar:

- velocidade;
- força;
- câmera;
- sensação de impacto;
- controles.

Polimento visual e de sensação previsto:

- efeitos de impacto nos contatos com a bola;
- som da bola;
- som de passos;
- som da rede;
- pequenas vibrações de câmera;
- trail da bola;
- feedback visual e sonoro de ação válida;
- animação de vitória e derrota do ponto;
- melhoria da iluminação da quadra.

Esses elementos devem reforçar a leitura e a sensação do gameplay sem alterar o balanceamento já validado.

---

## FASE 9 — EQUIPES

Migrar progressivamente:

1x1
↓
2x2
↓
4x4
↓
6x6

Não saltar diretamente para IA 6x6.

Primeira etapa — 2x2:

- responsabilidade pela bola;
- jogador de apoio;
- recepção direcionada para o companheiro;
- levantamento;
- ataque em equipe;
- posicionamento ofensivo;
- posicionamento defensivo;
- retorno dos jogadores às posições base.

Progressão obrigatória:

1. estabilizar o 2x2;
2. migrar e validar o 4x4;
3. somente depois iniciar o 6x6.

Cada formato deve estar funcional antes da expansão para o próximo.

---

## FASE 10 — IA TÁTICA

Adicionar:

- posições;
- cobertura de espaço;
- seleção de quem recebe;
- seleção de quem levanta;
- seleção de quem ataca;
- organização de bloqueio;
- retorno à formação;
- decisões específicas por posição;
- níveis de dificuldade da IA.

Fluxo esperado da IA de equipe:

Bola
↓
Previsão da trajetória
↓
Escolha do responsável
↓
Recepção
↓
Levantamento
↓
Ataque
↓
Cobertura ou retorno à formação

A dificuldade deve modificar tempos de reação, precisão e qualidade das decisões sem permitir movimentos ou alcances fisicamente impossíveis.

---

## FASE 11 — ANIMAÇÕES

Substituir animações provisórias.

---

## FASE 12 — INTERFACE

Criar:

- menus;
- placar;
- pausa;
- configurações.

---

## FASE 13 — CONTEÚDO

Criar:

- times;
- jogadores;
- uniformes;
- ginásios.

---

## FASE 14 — MULTIPLAYER

Somente quando gameplay offline estiver estável.

---

# PRINCÍPIO DO PROJETO

Sempre construir primeiro a versão mais simples que permita testar a mecânica.

Exemplo:

Não criar sistema completo de saque viagem antes de existir um saque simples funcional.

Não criar bloqueio triplo antes de existir bloqueio individual.

Não criar IA de seis jogadores antes de uma IA conseguir jogar 1x1.

Gameplay funcional primeiro.

Complexidade depois.
