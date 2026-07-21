import math

path = "fox.obj"

verts = []

with open(path) as f:
    for line in f:
        if line.startswith("v "):
            _,x,y,z = line.split()
            verts.append((float(x),float(y),float(z)))

print("Vertices:", len(verts))

xs=[v[0] for v in verts]
ys=[v[1] for v in verts]
zs=[v[2] for v in verts]

print("X:",min(xs),max(xs))
print("Y:",min(ys),max(ys))
print("Z:",min(zs),max(zs))

center=(
    sum(xs)/len(xs),
    sum(ys)/len(ys),
    sum(zs)/len(zs)
)

print("Center:",center)

radius=max(
    math.sqrt(
        x*x+y*y+z*z
    )
    for x,y,z in verts
)

print("Radius:",radius)