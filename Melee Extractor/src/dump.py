import clr
import os
import json
import System


# ============================================================
# LOAD HSDRAW
# ============================================================

BASE_DIR = os.path.dirname(os.path.abspath(__file__))

DLL = os.path.join(BASE_DIR, "HSDRaw.dll")

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


MAX_DEPTH = 20


# ============================================================
# GLOBAL
# ============================================================

visited=set()

export=[]


flags = (
    System.Reflection.BindingFlags.Public |
    System.Reflection.BindingFlags.NonPublic |
    System.Reflection.BindingFlags.Instance
)



# ============================================================
# SAFE HELPERS
# ============================================================


def get_type(obj):

    try:
        return obj.GetType()
    except:
        return None



def type_name(obj):

    t=get_type(obj)

    if t:
        return str(t)

    return str(type(obj))



def primitive(obj):

    if obj is None:
        return True


    if isinstance(
        obj,
        (
            int,
            float,
            str,
            bool
        )
    ):
        return True


    t=get_type(obj)

    if t is None:
        return True


    try:
        return t.IsPrimitive or t == System.String
    except:
        return False



def safe_value(v):

    if v is None:
        return None


    if primitive(v):

        try:
            return str(v)
        except:
            return None


    return {
        "__type__":type_name(v)
    }



# ============================================================
# REFLECTION
# ============================================================


def members(obj):

    out=[]

    t=get_type(obj)

    if t is None:
        return out


    try:

        for f in t.GetFields(flags):

            try:

                out.append(
                    (
                        str(f.Name),
                        f.GetValue(obj)
                    )
                )

            except:
                pass


    except:
        pass



    try:

        for p in t.GetProperties(flags):

            try:

                if p.CanRead:

                    out.append(
                        (
                            str(p.Name),
                            p.GetValue(obj,None)
                        )
                    )


            except:
                pass


    except:
        pass



    return out




# ============================================================
# WALKER
# ============================================================


def walk(obj,path,depth=0):


    if obj is None:
        return None


    if depth > MAX_DEPTH:
        return None


    if primitive(obj):
        return None



    oid=id(obj)


    if oid in visited:
        return None


    visited.add(oid)



    node={

        "path":path,

        "type":type_name(obj),

        "fields":{}

    }



    print(
        depth,
        path,
        node["type"]
    )



    export.append(node)



    for name,value in members(obj):


        # primitive data

        if primitive(value):

            node["fields"][name]=safe_value(value)

            continue



        # arrays

        try:

            if isinstance(value,System.Array):

                arr=[]


                for i,item in enumerate(value):

                    child=walk(
                        item,
                        path+"."+name+"["+str(i)+"]",
                        depth+1
                    )

                    if child:
                        arr.append(child)


                node["fields"][name]={
                    "array":arr
                }


                continue


        except:
            pass



        child=walk(
            value,
            path+"."+name,
            depth+1
        )


        if child:

            node["fields"][name]=child



    return node




# ============================================================
# RUN
# ============================================================


for dat in DAT_FILES:


    if not os.path.exists(dat):

        print(
            "MISSING",
            dat
        )

        continue



    print()
    print("======================")
    print("OPENING",dat)
    print("======================")


    visited.clear()



    f=HSDRawFile(dat)



    for root in f.Roots:


        print(
            "ROOT",
            root.Name,
            root.GetType()
        )


        walk(
            root.Data,
            str(root.Name)
        )




# ============================================================
# SAVE
# ============================================================


print()
print("======================")
print("WRITING EXPORT")
print("======================")


with open(
    "fox_full_dump.json",
    "w",
    encoding="utf8"
) as fp:


    json.dump(
        export,
        fp,
        indent=2
    )


print(
    "DONE",
    len(export),
    "objects"
)