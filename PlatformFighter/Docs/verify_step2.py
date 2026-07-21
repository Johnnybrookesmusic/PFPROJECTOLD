"""Mirror of SegmentCollisionResolver.cs, run against the real Battlefield data,
to validate the ALGORITHM (not the C#). Y-DOWN, feet-origin, same as the engine."""

EPS = 1.0 / 1024

# Battlefield, Y already negated to Y-down (matching Stages/Battlefield.cs)
GROUND = [((-68.4, 0.0), (68.4, 0.0))]
PLATFORMS = [(-27.2, -57.6, -20.0), (-27.2, 20.0, 57.6), (-54.4, -18.8, 18.8)]  # (y, xmin, xmax)
CEILING = [((-65.0, 6.0), (-36.0, 19.0)), ((-29.0, 35.0), (-10.0, 40.0)),
           ((-10.0, 30.0), (10.0, 30.0)), ((65.0, 6.0), (36.0, 19.0)),
           ((29.0, 35.0), (10.0, 40.0))]
BLAST_X, BLAST_TOP, BLAST_BOT = 224.0, -200.0, 108.8

# Fox ECB WAIT [4,3,9,13]
ECB_DROP, ECB_HW, ECB_SIDE, ECB_TOP = 4.0, 3.0, 9.0, 13.0

# Fox attributes (MeleeLight)
GRAV, TERM, FASTFALL = 0.23, 2.8, 3.4
FHOP, SHOP, DJ = 3.68, 2.1, 3.68 * 1.2
WALK, DASH, TRACTION = 1.6, 2.02, 0.08


def spans_x(seg, x):
    return min(seg[0][0], seg[1][0]) <= x <= max(seg[0][0], seg[1][0])


def y_at(seg, x):
    dx = seg[1][0] - seg[0][0]
    if dx == 0:
        return None
    t = (x - seg[0][0]) / dx
    return seg[0][1] + (seg[1][1] - seg[0][1]) * t


def resolve(prev, moved, vel, grounded, surface, index, pass_through):
    px, py = prev
    x, y = moved
    vx, vy = vel
    out = dict(pos=(x, y), vel=(vx, vy), grounded=False, surface=None,
               index=-1, ceil=False, wall=False, edge=False)

    # ceilings (upward only)
    if vy < 0:
        prev_top, new_top = py - ECB_TOP, y - ECB_TOP
        for seg in CEILING:
            lo, hi = min(seg[0][0], seg[1][0]), max(seg[0][0], seg[1][0])
            if x + ECB_HW < lo or x - ECB_HW > hi:
                continue
            cy = y_at(seg, max(lo, min(x, hi)))
            if cy is None or prev_top < cy or new_top > cy:
                continue
            y = cy + ECB_TOP + EPS
            vy = 0.0
            out['ceil'] = True
            break

    # stay on surface -- but NOT if moving upward (a jump has left the ground)
    if grounded and surface is not None and vy >= 0:
        if surface == 'g':
            seg = GROUND[index]
            if spans_x(seg, x):
                sy = y_at(seg, x)
                return dict(pos=(x, sy + EPS), vel=(vx, 0.0), grounded=True,
                            surface='g', index=index, ceil=out['ceil'], wall=False, edge=False)
        elif not pass_through:
            py_, xmin, xmax = PLATFORMS[index]
            if xmin <= x <= xmax:
                return dict(pos=(x, py_ + EPS), vel=(vx, 0.0), grounded=True,
                            surface='p', index=index, ceil=out['ceil'], wall=False, edge=False)
        out['edge'] = True

    # land
    if vy >= 0:
        prev_bottom, new_bottom = py + ECB_DROP, y + ECB_DROP
        best = None
        for i, seg in enumerate(GROUND):
            if not spans_x(seg, x):
                continue
            sy = y_at(seg, x)
            if prev_bottom > sy or new_bottom < sy:
                continue
            if best is None or sy < best[0]:
                best = (sy, 'g', i)
        if not pass_through:
            for i, (ply, xmin, xmax) in enumerate(PLATFORMS):
                if not (xmin <= x <= xmax):
                    continue
                if prev_bottom > ply or new_bottom < ply:
                    continue
                if best is None or ply < best[0]:
                    best = (ply, 'p', i)
        if best:
            sy, kind, i = best
            return dict(pos=(x, sy + EPS), vel=(vx, 0.0), grounded=True,
                        surface=kind, index=i, ceil=out['ceil'], wall=False, edge=False)

    out['pos'] = (x, y)
    out['vel'] = (vx, vy)
    return out


def sim(x0, y0, vx0, vy0, grounded, surface, index, ticks, fastfall=False, hold_x=0.0):
    x, y, vx, vy = x0, y0, vx0, vy0
    log = []
    for t in range(ticks):
        prev = (x, y)
        if not grounded:
            vy += GRAV
            cap = FASTFALL if fastfall else TERM
            vy = min(vy, cap)
        else:
            if hold_x == 0:
                vx = max(0.0, abs(vx) - TRACTION) * (1 if vx > 0 else -1)
            else:
                vx = max(-WALK, min(WALK, vx + 0.2 * hold_x))
        x, y = x + vx, y + vy
        r = resolve(prev, (x, y), (vx, vy), grounded, surface, index, False)
        x, y = r['pos']; vx, vy = r['vel']
        grounded, surface, index = r['grounded'], r['surface'], r['index']
        log.append((t + 1, round(x, 3), round(y, 3), round(vy, 3), grounded, surface, r['edge']))
    return log, (x, y, vx, vy, grounded, surface, index)


print("=" * 68)
print("TEST 1 - full hop from centre ground, must leave ground and resettle")
log, end = sim(0.0, 0.0, 0.0, -FHOP, True, 'g', 0, 60)
apex = min(l[2] for l in log)
landed = [l for l in log if l[4]]
print(f"  apex height above ground : {-apex:.2f} units")
print(f"  airborne ticks           : {sum(1 for l in log if not l[4])}")
print(f"  final grounded           : {end[4]}  y={end[1]:.4f}  surface={end[5]}")
assert -apex > 25, "full hop should clear ~29 units"
assert end[4], "must land"
print("  PASS")

print()
print("=" * 68)
print("TEST 2 - walk off the right ledge: must fall, not hover")
log, end = sim(60.0, 0.0, DASH, 0.0, True, 'g', 0, 40, hold_x=1.0)
edge_tick = next((l[0] for l in log if l[6]), None)
print(f"  walked-off-edge fired at tick : {edge_tick}")
print(f"  final: x={end[0]:.1f} y={end[1]:.1f} grounded={end[4]}")
assert edge_tick is not None, "should detect the edge"
assert not end[4], "should be falling off-stage"
assert end[1] > 30, "should be well below stage level"
print("  PASS")

print()
print("=" * 68)
print("TEST 3 - fall onto a side platform (topmost-surface-wins)")
log, end = sim(-40.0, -80.0, 0.0, 0.0, False, None, -1, 40)
first_land = next((l for l in log if l[4]), None)
print(f"  landed at tick {first_land[0]} on surface '{first_land[5]}' y={first_land[2]:.3f}")
assert first_land[5] == 'p', "should land on the PLATFORM, not fall through to ground"
assert abs(first_land[2] - (-27.2)) < 0.01, "platform is at y=-27.2"
print("  PASS")

print()
print("=" * 68)
print("TEST 4 - drop through a platform when passing")
x, y, vy = -40.0, -27.2, 0.0
grounded, surface, index = True, 'p', 0
for t in range(40):
    prev = (x, y)
    vy = min(vy + GRAV, TERM)
    y += vy
    r = resolve(prev, (x, y), (0.0, vy), grounded, surface, index, pass_through=True)
    x, y = r['pos']; vy = r['vel'][1]
    grounded, surface, index = r['grounded'], r['surface'], r['index']
    if grounded:
        break
print(f"  ended grounded={grounded} on '{surface}' at y={y:.3f} after {t+1} ticks")
assert surface == 'g' and abs(y) < 0.01, "should drop through platform and land on ground"
print("  PASS")

print()
print("=" * 68)
print("TEST 5 - determinism: identical inputs give bit-identical results")
a = sim(0.0, 0.0, 0.0, -FHOP, True, 'g', 0, 60)[0]
b = sim(0.0, 0.0, 0.0, -FHOP, True, 'g', 0, 60)[0]
assert a == b
print("  PASS")

print()
print("=" * 68)
print("SCALE SANITY (why Step 2 exists)")
print(f"  Fox ECB width  : {2*ECB_HW}  = {100*2*ECB_HW/136.8:.1f}% of stage")
print(f"  old HalfSize   : {40}  = {100*40/136.8:.1f}% of stage  <-- the bug")
print(f"  Fox ECB height : {ECB_TOP}   vs old {60}")
print(f"  top platform is {54.4:.1f} up; Fox full hop reaches {-apex:.1f} -> "
      f"{'reachable' if -apex > 54.4 else 'NOT reachable from ground alone (needs double jump - correct for Melee)'}")
