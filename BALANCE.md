# Prototype 1x1 Balance Baseline

Values recorded from `PrototypeCourt.unity` and its attached scripts. No subjective gameplay values were changed during this phase.

## Player

- Move Speed: 5 m/s
- Run Speed: 8 m/s
- Acceleration: 20 m/s²
- Deceleration: 25 m/s²
- Rotation Speed: 12
- Jump Height: 1.2 m
- Air Control: 0.75
- Gravity Multiplier: 1.7
- Fall Multiplier: 1.3

## Ball

- Mass: 0.27 kg
- Linear Damping: 0.08
- Angular Damping: 0.08
- Maximum Speed: 20 m/s

## Receive

- Controlled Flight Time: 1.15 s
- Controlled Target: PlayerControlTarget, clamped to Player side
- Direct Return Input: LeftAlt + E
- Direct Return Flight Time: 1.2–1.8 s
- Direct Return Net Clearance: 0.45 m
- Contact Height: 0.3–1.7 m
- Shared Contact Radius: 1.1 m

## Set

- Force: 2.35
- Vertical Component: 1.2
- Forward Component: 0.18
- Input Buffer: 0.15 s
- Contact Height: 0.65–2.5 m
- Minimum Forward Dot: -0.20

## Spike

- Force: 4
- Forward Bias: 1
- Downward Bias: 0.12
- Contact Height: 1.35–3.1 m
- Minimum Forward Dot: 0.08
- Action Window: 0.35 s
- Input Buffer: 0.20 s

## Block

- Force: 1.2
- Downward Bias: 0.2
- Action Window: 0.3 s
- Contact Area: 1.4 × 1 × 0.5 m

## Serve

- Force: 3.3
- Vertical Bias: 0.7
- Maximum Serve Distance: 3.5 m

## AI Opponent

- Move Speed: 5 m/s
- Acceleration: 16 m/s²
- Deceleration: 20 m/s²
- Reaction Time: 0.18 s
- Positioning Offset: 0.8 m
- Attack Chance: 0.35
- Target Variation: 0.75 m
- Contact Cooldown: 0.35 s
- Receive Trajectory: ballistic, 1.2-2.0 s to the safe return area
- Attack Force: 2.1

## Camera

- Player Pivot Height: 1.4 m
- Height: 2.5 m
- Distance: 4.8 m
- Position Smooth Time: 0.1 s
- Rotation Smooth Time: 0.08 s
- FOV: 64°
- Ball Assist Weight: 0.30

# Playtest Checklist

## Movement

- [ ] Movimento responsivo
- [ ] Mudança de direção adequada
- [ ] Salto confortável
- [ ] Controle aéreo adequado

## Ball

- [ ] Trajetória previsível
- [ ] Velocidade adequada
- [ ] Quique adequado

## Receive

- [ ] Fácil de entender
- [ ] Alcance justo
- [ ] Trajetória adequada

## Set

- [ ] Altura suficiente
- [ ] Bola atacável

## Spike

- [ ] Passa pela rede
- [ ] Cai dentro da quadra
- [ ] Sensação ofensiva

## Block

- [ ] Janela justa
- [ ] Alcance justo

## Serve

- [ ] Consistente
- [ ] Cai dentro da quadra

## AI

- [ ] Reage naturalmente
- [ ] Não teleporta
- [ ] Não alcança tudo
- [ ] Mantém rallies
- [ ] Pode perder pontos

## Camera

- [ ] Bola permanece visível
- [ ] Player permanece legível
- [ ] Movimento confortável

# Evaluation Scale

Use this scale for each item being evaluated:

- [ ] Muito fraco
- [ ] Fraco
- [ ] Bom
- [ ] Forte
- [ ] Muito forte

Suggested categories: movement response, jump, receive height, set height, spike power, block reach, serve power, AI reaction and camera comfort.

# Problems Found

`[Sistema] problema observado`

# Balance Changes

Format: `Data | sistema | valor anterior → valor novo | motivo`

2026-09-21 | AI Attack Chance | 0.65 -> 0.35 | Prioriza devolucoes controladas e rallies mais longos.

2026-09-21 | AI Receive | impulso minimo para cruzar a rede -> alvo seguro X [-3, 3], Z [-7, -3.5] com voo balistico de 1.2-2.0 s | Da ao Player tempo e espaco previsiveis para continuar o rally.

2026-09-21 | AI Receive Force / Vertical Bias | 1.6 / 0.9 -> velocidade calculada pelo alvo e tempo de voo | Evita que uma forca fixa produza devolucoes baixas ou inalcançaveis.

2026-09-21 | Camera | top-down: height 9.5, distance 11, FOV 48-54 -> third-person: height 2.5, distance 4.8, FOV 64 | Reforca o controle individual do atleta e a leitura da acao a frente.

2026-09-21 | Receive | devolucao unica para frente -> E Controlled Receive no proprio lado / LeftAlt+E Direct Return | Playtest mostrou que Receive devolvia automaticamente para o adversario, impedindo construcao intencional de jogadas.

2026-09-21 | Set | sem buffer, height 0.8-2.3, forward dot -0.15 -> buffer 0.15 s, height 0.65-2.5, forward dot -0.20 | Torna Q utilizavel pouco antes da bola entrar na zona.

2026-09-21 | Spike | window 0.25, buffer 0.15, height 1.5-3.0, forward dot 0.15 -> window 0.35, buffer 0.20, height 1.35-3.1, forward dot 0.08 | Amplia moderadamente o timing sem remover a exigencia de estar no ar.

2026-09-21 | Controlled Receive | flight time 0.85 s, target height 1.3 m -> flight time 1.15 s, target height 1.6 m | A recepcao anterior permanecia baixa demais para preparar o levantamento.

2026-09-21 | Set | height 0.65-2.5, angle -0.20, input buffer 0.15 s -> valores preservados; alvo relativo 1.4 m acima/a frente e voo de 1.0 s | Polimento do fluxo ofensivo apos estabilizacao da recepcao.

2026-09-21 | Spike | height 1.35-3.1, angle 0.08, window 0.35 s, input buffer 0.20 s -> valores preservados; direcao limita componente ao adversario e clearance de rede 0.25 m | Polimento do fluxo ofensivo apos estabilizacao da recepcao.

2026-09-21 | Set | base force 2.35, alvo 1.4 m relativo ao Player -> base force 2.35 preservada, PlayerAttackSetTarget calculado a 1.4 m da rede, target height 2.25 m e apex 3.3 m | Set precisava preparar melhor o ataque.

2026-09-21 | Spike | base force 4 e direcao por impulso -> base force 4 como limite, alvo balistico com safe area X [-3.9, 3.9] / Z [1.0, 8.2], net clearance 0.25 m e assistencia por qualidade do contato | Spikes estavam ultrapassando sistematicamente o fundo da quadra.

2026-09-21 | Movement | sem limite compartilhado -> Player X [-4.25, 4.25] / Z [-8.5, -0.35], Opponent X [-4.25, 4.25] / Z [0.35, 8.5], net buffer 0.35 m | Jogadores nao devem invadir fisicamente o lado adversario.

2026-09-21 | AI Defense Reaction | normal 0.18 s -> normal 0.18 s / fast ball 0.10 s | Playtest mostrou resposta defensiva excessivamente lenta contra Spike.

2026-09-21 | AI Defense Movement | move speed 5.0 -> 5.5; acceleration 16 -> 22; deceleration 20 preservada | Playtest mostrou resposta defensiva excessivamente lenta contra Spike.

2026-09-21 | AI Defense Prediction | normal threshold 0.25 m -> normal 0.25 m / fast ball 0.12 m; fast incoming threshold 9 m/s | Playtest mostrou resposta defensiva excessivamente lenta contra Spike.

2026-09-21 | AI Defense Reach | position tolerance normal 0.30 m -> normal 0.30 m / urgent 0.40 m; reachability margin 1.10; receive distance 1.10 m -> 1.20 m | Playtest mostrou resposta defensiva excessivamente lenta contra Spike.

2026-09-21 | AI Receive Contact | sphere radius 1.05 m / offset (0, 0.6, 0.9) -> radius 1.20 m / offset (0, 0.8, 0.25); receive angle sem validacao fisica -> -0.35; height sem validacao fisica -> 0.25-2.0 m; emergency radius 0.85 m; close distance 0.65 m | Playtest mostrou falha de contato da IA mesmo com bola fisicamente proxima.
