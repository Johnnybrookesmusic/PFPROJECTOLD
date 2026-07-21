import clr
import System

clr.AddReference("HSDRaw")

import HSDRaw


print("====================")
print("SEARCHING HSDRAW METHODS")
print("====================")


assemblies = System.AppDomain.CurrentDomain.GetAssemblies()


for assembly in assemblies:

    if "HSDRaw" not in str(assembly.FullName):
        continue


    print()
    print("====================")
    print(
        "ASSEMBLY:",
        assembly.FullName
    )
    print("====================")


    for typ in assembly.GetTypes():

        name = str(typ.FullName)


        if (
            "Loader" in name
            or "Accessor" in name
            or "Struct" in name
            or "Parser" in name
            or "Serializer" in name
        ):

            print()
            print(name)


            try:

                for method in typ.GetMethods():

                    method_name = str(method.Name)


                    if (
                        "New" in method_name
                        or "Load" in method_name
                        or "Read" in method_name
                        or "Parse" in method_name
                        or "From" in method_name
                    ):

                        print(
                            "   ",
                            method_name
                        )

            except Exception:

                pass