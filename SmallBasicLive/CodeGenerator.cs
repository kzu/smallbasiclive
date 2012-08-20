using System;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.SmallBasic;
using Microsoft.SmallBasic.Library;
using Microsoft.SmallBasic.Statements;

namespace SmallBasicLive
{
    public class CodeGenerator
    {
        private CodeGenScope _currentScope;
        private MethodInfo _entryPoint;
        private Parser _parser;
        private SymbolTable _symbolTable;
        private TypeInfoBag _typeInfoBag;

        public CodeGenerator(Parser parser, TypeInfoBag typeInfoBag)
        {
            this._parser = parser;
            this._symbolTable = this._parser.SymbolTable;
            this._typeInfoBag = typeInfoBag;
        }

        private void BuildFields(TypeBuilder typeBuilder)
        {
            foreach (string str in this._parser.SymbolTable.Variables.Keys)
            {
                FieldInfo info = typeBuilder.DefineField(str, typeof(Primitive), FieldAttributes.Static | FieldAttributes.Private);
                this._currentScope.Fields.Add(str, info);
            }
        }

        private void EmitIL()
        {
            foreach (Statement statement in this._parser.ParseTree)
            {
                statement.PrepareForEmit(this._currentScope);
            }
            foreach (Statement statement2 in this._parser.ParseTree)
            {
                statement2.EmitIL(this._currentScope);
            }
        }

        private bool EmitModule(ModuleBuilder moduleBuilder)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Program", TypeAttributes.Sealed | TypeAttributes.Public);
            MethodBuilder builder2 = typeBuilder.DefineMethod("Run", MethodAttributes.Static | MethodAttributes.Public);
            this._entryPoint = builder2;
            ILGenerator iLGenerator = builder2.GetILGenerator();
            CodeGenScope scope = new CodeGenScope
            {
                ILGenerator = iLGenerator,
                MethodBuilder = builder2,
                TypeBuilder = typeBuilder,
                SymbolTable = this._symbolTable,
                TypeInfoBag = this._typeInfoBag
            };
            this._currentScope = scope;
            this.BuildFields(typeBuilder);
            iLGenerator.EmitCall(OpCodes.Call, typeof(GraphicsWindow).GetMethod("Clear", BindingFlags.Public | BindingFlags.Static), null);
            this.EmitIL();
            iLGenerator.Emit(OpCodes.Ret);
            typeBuilder.CreateType();
            return true;
        }

        public Assembly GenerateAssembly()
        {
            try
            {
                AssemblyName name = new AssemblyName
                {
                    Name = Guid.NewGuid().ToString()
                };
                AssemblyBuilder builder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
                ModuleBuilder moduleBuilder = builder.DefineDynamicModule(name.Name + ".exe", true);
                if (!this.EmitModule(moduleBuilder))
                {
                    return null;
                }

                return builder;
            }
            catch
            {
                return null;
            }
        }
    }
}
