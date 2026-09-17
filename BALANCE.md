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

- Force: 2.5
- Vertical Component: 1.15
- Horizontal Multiplier: 0.65
- Contact Height: 0.3–1.7 m
- Shared Contact Radius: 1.1 m

## Set

- Force: 2.35
- Vertical Component: 1.2
- Forward Component: 0.18
- Contact Height: 0.8–2.3 m

## Spike

- Force: 4
- Forward Bias: 1
- Downward Bias: 0.12
- Contact Height: 1.5–3 m
- Action Window: 0.25 s
- Input Buffer: 0.15 s

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
- Attack Chance: 0.65
- Target Variation: 0.75 m
- Contact Cooldown: 0.35 s
- Receive Force: 1.6
- Attack Force: 2.1

## Camera

- Height: 9.5 m
- Distance: 11 m
- Position Smooth Time: 0.2 s
- Rotation Smooth Time: 0.15 s
- FOV Range: 48–54°

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

No balance changes recorded yet.
