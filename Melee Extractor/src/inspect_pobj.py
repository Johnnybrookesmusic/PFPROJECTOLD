import clr
import os
import json
import System


# ============================================================
# LOAD HSDRAW
# ============================================================

BASE_DIR = os.path.dirname(
    os.path.abspath(__file__)
)

DLL = os.path.join(
    BASE_DIR,
    "HSDRaw.dll"
)

if not os.path.exists(DLL):
    raise FileNotFoundError(DLL)


clr.AddReference(DLL)

from HSDRaw import HSDRawFile



# ============================================================
# CONFIG
# ============================================================

DAT_FILES = [
    "PlFx.dat",
    "PlFc.dat",
    "PlCo.dat",
    "PlFxAJ.dat"
]


MAX_DEPTH = 12


# ============================================================
# GLOBALS
# ============================================================

visited = set()

results = []

flags = (
    System.Reflection.BindingFlags.Public |
    System.Reflection.BindingFlags.NonPublic |
    System.Reflection.BindingFlags.Instance
)



# ============================================================
# SAFE TYPE HELPERS
# ============================================================


def get_type(obj):

    if obj is None:
        return None

    try:
        return obj.GetType()

    except:
        return None



def typename(obj):

    t = get_type(obj)

    if t:

        return str(t)

    return str(type(obj))



def is_primitive(obj):

    if obj is None:
        return True


    # Python primitives

    if isinstance(
        obj,
        (
            int,
            float,
            bool,
            str
        )
    ):
        return True


    # .NET primitives

    t = get_type(obj)

    if t is None:
        return True


    return (
        t.IsPrimitive
        or
        t == System.String
    )



# ============================================================
# REFLECTION
# ============================================================


def get_members(obj):

    output=[]


    t=get_type(obj)


    if t is None:
        return output



    # fields

    try:

        for f in t.GetFields(flags):

            try:

                value=f.GetValue(obj)

                output.append(
                    (
                        str(f.Name),
                        value
                    )
                )

            except:
                pass

    except:
        pass



    # properties

    try:

        for p in t.GetProperties(flags):

            try:

                if not p.CanRead:
                    continue


                value=p.GetValue(
                    obj,
                    None
                )


                output.append(
                    (
                        str(p.Name),
                        value
                    )
                )


            except:
                pass


    except:
        pass



    return output



# ============================================================
# HSD REFERENCE WALKER
# ============================================================


def scan_hsdstruct(obj,path,depth):

    try:

        field=obj.GetType().GetField(
            "_references",
            flags
        )


        if field is None:
            return


        refs=field.GetValue(obj)


        if refs is None:
            return



        for entry in refs:

            try:

                walk(
                    entry.Value,
                    path+
                    "._ref["+
                    str(entry.Key)+
                    "]",
                    depth+1
                )

            except:
                pass


    except:
        pass



# ============================================================
# MAIN WALKER
# ============================================================


def walk(obj,path,depth=0):


    if obj is None:
        return


    if depth > MAX_DEPTH:
        return



    if is_primitive(obj):
        return



    oid=id(obj)


    if oid in visited:
        return


    visited.add(oid)



    t=typename(obj)



    interesting=[
        "JOBJ",
        "POBJ",
        "MOBJ",
        "DOBJ",
        "Hit",
        "Hurt",
        "Collision",
        "Action",
        "Animation",
        "Model",
        "Bone",
        "Article",
        "Physics"
    ]


    for target in interesting:


        if target.lower() in t.lower():


            print()
            print("======================")
            print("FOUND")
            print(path)
            print(t)
            print("======================")


            results.append(
                {
                    "path":path,
                    "type":t
                }
            )

            break



    # HSDStruct references

    if "HSDStruct" in t:

        scan_hsdstruct(
            obj,
            path,
            depth
        )



    # children

    for name,value in get_members(obj):


        if value is None:
            continue


        if is_primitive(value):
            continue



        # arrays

        try:

            if isinstance(
                value,
                System.Array
            ):


                for i,item in enumerate(value):

                    walk(
                        item,
                        path+
                        "."+
                        name+
                        "["+
                        str(i)+
                        "]",
                        depth+1
                    )


                continue


        except:
            pass



        walk(
            value,
            path+
            "."+
            name,
            depth+1
        )



# ============================================================
# RUN
# ============================================================


for DAT in DAT_FILES:


    if not os.path.exists(DAT):

        print(
            "SKIP",
            DAT
        )

        continue



    print()
    print("==============================")
    print("LOADING")
    print(DAT)
    print("==============================")


    visited.clear()



    file=HSDRawFile(DAT)



    for root in file.Roots:


        print(
            "ROOT:",
            root.Name,
            root.GetType()
        )


        walk(
            root.Data,
            str(root.Name)
        )



# ============================================================
# EXPORT
# ============================================================


print()
print("==============================")
print("EXPORT")
print("==============================")


with open(
    "fox_scan.json",
    "w"
) as f:


    json.dump(
        results,
        f,
        indent=2
    )



print(
    "DONE"
)

print(
    "FOUND OBJECTS:",
    len(results)
)