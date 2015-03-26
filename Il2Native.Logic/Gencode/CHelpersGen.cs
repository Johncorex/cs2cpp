﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LlvmHelpersGen.cs" company="">
//   
// </copyright>
// <summary>
//   
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Il2Native.Logic.Gencode
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using CodeParts;
    using Exceptions;
    using InternalMethods;
    using PEAssemblyReader;
    using OpCodesEmit = System.Reflection.Emit.OpCodes;

    /// <summary>
    /// </summary>
    public static class CHelpersGen
    {
        /// <summary>
        /// </summary>
        /// <param name="cWriter">
        /// </param>
        /// <param name="opCodeMethodInfo">
        /// </param>
        /// <param name="methodInfo">
        /// </param>
        /// <param name="thisType">
        /// </param>
        /// <param name="opCodeFirstOperand">
        /// </param>
        /// <param name="resultOfirstOperand">
        /// </param>
        /// <param name="requiredType">
        /// </param>
        /// <returns>
        /// </returns>
        public static void GenerateVirtualCall(
            this CWriter cWriter,
            OpCodePart opCodeMethodInfo,
            IMethod methodInfo,
            IType thisType,
            OpCodePart opCodeFirstOperand,
            BaseWriter.ReturnResult resultOfirstOperand,
            ref IType requiredType)
        {
            var writer = cWriter.Output;

            if (thisType.IsInterface && resultOfirstOperand.Type.TypeNotEquals(thisType))
            {
                // we need to extract interface from an object
                requiredType = thisType;
            }

            // get pointer to Virtual Table and call method
            // 1) get pointer to virtual table
            IType requiredInterface;
            var effectiveType = requiredType ?? thisType;
            effectiveType.GetVirtualMethodIndexAndRequiredInterface(methodInfo, cWriter, out requiredInterface);

            writer.Write("(*(");
            writer.Write("({0}*)", thisType.GetVirtualTableName(cWriter));

            if (requiredInterface != null || effectiveType.IsInterface)
            {
                cWriter.WriteFieldAccess(writer, opCodeMethodInfo, (requiredInterface ?? effectiveType).GetInterfaceVTable(cWriter));
            }
            else
            {
                cWriter.WriteFieldAccess(writer, opCodeMethodInfo, cWriter.System.System_Object.GetFieldByName("vtable", cWriter));
            }

            writer.Write(")->");
            var methodName = methodInfo.ToString(null, true).CleanUpName();
            writer.Write(methodName);
            writer.Write(")");
        }

        public static IType GetIntTypeByBitSize(this BaseWriter llvmWriter, int bitSize)
        {
            IType toType = null;
            switch (bitSize)
            {
                case 1:
                    toType = llvmWriter.System.System_Boolean;
                    break;
                case 8:
                    toType = llvmWriter.System.System_SByte;
                    break;
                case 16:
                    toType = llvmWriter.System.System_Int16;
                    break;
                case 32:
                    toType = llvmWriter.System.System_Int32;
                    break;
                case 64:
                    toType = llvmWriter.System.System_Int64;
                    break;
            }

            return toType;
        }

        /// <summary>
        /// </summary>
        /// <param name="typeResolver">
        /// </param>
        /// <param name="byteSize">
        /// </param>
        /// <returns>
        /// </returns>
        public static IType GetIntTypeByByteSize(this ITypeResolver typeResolver, int byteSize)
        {
            IType toType = null;
            switch (byteSize)
            {
                case 1:
                    toType = typeResolver.System.System_SByte;
                    break;
                case 2:
                    toType = typeResolver.System.System_Int16;
                    break;
                case 4:
                    toType = typeResolver.System.System_Int32;
                    break;
                case 8:
                    toType = typeResolver.System.System_Int64;
                    break;
            }

            return toType;
        }

        public static IType GetUIntTypeByBitSize(this ITypeResolver llvmWriter, int bitSize)
        {
            IType toType = null;
            switch (bitSize)
            {
                case 1:
                    toType = llvmWriter.System.System_Boolean;
                    break;
                case 8:
                    toType = llvmWriter.System.System_Byte;
                    break;
                case 16:
                    toType = llvmWriter.System.System_UInt16;
                    break;
                case 32:
                    toType = llvmWriter.System.System_UInt32;
                    break;
                case 64:
                    toType = llvmWriter.System.System_UInt64;
                    break;
            }

            return toType;
        }

        /// <summary>
        /// </summary>
        /// <param name="llvmWriter">
        /// </param>
        /// <param name="byteSize">
        /// </param>
        /// <returns>
        /// </returns>
        public static IType GetUIntTypeByByteSize(this ITypeResolver llvmWriter, int byteSize)
        {
            IType toType = null;
            switch (byteSize)
            {
                case 1:
                    toType = llvmWriter.System.System_Byte;
                    break;
                case 2:
                    toType = llvmWriter.System.System_UInt16;
                    break;
                case 4:
                    toType = llvmWriter.System.System_UInt32;
                    break;
                case 8:
                    toType = llvmWriter.System.System_UInt64;
                    break;
            }

            return toType;
        }

        /// <summary>
        /// </summary>
        /// <param name="cWriter">
        /// </param>
        /// <param name="opCode">
        /// </param>
        /// <param name="toType">
        /// </param>
        public static void WriteCCast(this CWriter cWriter, OpCodePart opCode, IType toType)
        {
            var writer = cWriter.Output;

            cWriter.WriteCCastOnly(toType);

            writer.Write("(");
            cWriter.WriteResultOrActualWrite(writer, opCode);
            writer.Write(")");
        }

        public static void WriteCCastOperand(this CWriter cWriter, OpCodePart opCode, int operand, IType toType)
        {
            var writer = cWriter.Output;

            cWriter.WriteCCastOnly(toType);

            writer.Write("(");
            cWriter.WriteOperandResultOrActualWrite(writer, opCode, operand);
            writer.Write(")");
        }

        public static void WriteCCastOnly(this CWriter cWriter, IType toType)
        {
            var writer = cWriter.Output;

            writer.Write("(");
            toType.WriteTypePrefix(cWriter);
            writer.Write(") ");
        }

        /// <summary>
        /// </summary>
        /// <param name="cWriter">
        /// </param>
        /// <param name="opCodeMethodInfo">
        /// </param>
        /// <param name="methodInfo">
        /// </param>
        /// <returns>
        /// </returns>
        public static bool ProcessPluggableMethodCall(
            this CWriter cWriter,
            OpCodePart opCodeMethodInfo,
            IMethod methodInfo)
        {
            if (methodInfo.HasProceduralBody)
            {
                var customAction = methodInfo as IMethodBodyCustomAction;
                if (customAction != null)
                {
                    customAction.Execute(cWriter, opCodeMethodInfo);
                }

                return true;
            }

            // TODO: it seems, you can preprocess MSIL code and replace all functions with MSIL code blocks to stop writing the code manually.
            // for example call System.Activator.CreateInstance<X>() can be replace with "Code.NewObj x"
            // the same interlocked functions and the same for TypeOf operators
            if (methodInfo.IsTypeOfCallFunction() && opCodeMethodInfo.WriteTypeOfFunction(cWriter))
            {
                return true;
            }

            if (methodInfo.IsInterlockedFunction())
            {
                methodInfo.WriteInterlockedFunction(opCodeMethodInfo, cWriter);
                return true;
            }

            if (methodInfo.IsThreadingFunction())
            {
                methodInfo.WriteThreadingFunction(opCodeMethodInfo, cWriter);
                return true;
            }

            if (methodInfo.IsActivatorFunction())
            {
                methodInfo.WriteActivatorFunction(opCodeMethodInfo, cWriter);
                return true;
            }

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="opCodePart">
        /// </param>
        /// <param name="label">
        /// </param>
        public static void SetCustomLabel(OpCodePart opCodePart, string label)
        {
            if (opCodePart.AddressStart == 0 && opCodePart.UsedBy != null)
            {
                opCodePart.UsedBy.OpCode.CreatedLabel = label;
            }
            else
            {
                opCodePart.CreatedLabel = label;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="cWriter">
        /// </param>
        /// <param name="opCode">
        /// </param>
        /// <param name="source">
        /// </param>
        /// <param name="toType">
        /// </param>
        public static void WriteCCast(
            this CWriter cWriter,
            OpCodePart opCode,
            FullyDefinedReference source,
            IType toType,
            bool asReference = true)
        {
            cWriter.WriteStartCCast(opCode, toType, asReference);
            cWriter.WriteResult(source);
            cWriter.WriteEndCCast(opCode, toType);
        }

        public static void WriteEndCCast(this CWriter cWriter, OpCodePart opCode, IType toType)
        {
            cWriter.Output.Write(")");
        }

        public static void WriteStartCCast(this CWriter cWriter, OpCodePart opCode, IType toType, bool asReference = false)
        {
            var writer = cWriter.Output;

            writer.Write("(");
            toType.WriteTypePrefix(cWriter, asReference);
            writer.Write(") (");
        }

        /// <summary>
        /// </summary>
        /// <param name="cWriter">
        /// </param>
        /// <param name="opCodeMethodInfo">
        /// </param>
        /// <param name="methodInfo">
        /// </param>
        /// <param name="isVirtual">
        /// </param>
        /// <param name="hasThis">
        /// </param>
        /// <param name="isCtor">
        /// </param>
        /// <param name="thisResultNumber">
        /// </param>
        /// <param name="tryClause">
        /// </param>
        public static void WriteCall(
            this CWriter cWriter,
            OpCodePart opCodeMethodInfo,
            IMethod methodInfo,
            bool isVirtual,
            bool hasThis,
            bool isCtor,
            FullyDefinedReference thisResultNumber,
            TryClause tryClause)
        {
            IType thisType;
            bool hasThisArgument;
            OpCodePart opCodeFirstOperand;
            BaseWriter.ReturnResult resultOfFirstOperand;
            bool isIndirectMethodCall;
            IType ownerOfExplicitInterface;
            IType requiredType;
            methodInfo.FunctionCallProlog(
                opCodeMethodInfo,
                isVirtual,
                hasThis,
                cWriter,
                out thisType,
                out hasThisArgument,
                out opCodeFirstOperand,
                out resultOfFirstOperand,
                out isIndirectMethodCall,
                out ownerOfExplicitInterface,
                out requiredType);

            FullyDefinedReference methodAddressResultNumber = null;
            if (isIndirectMethodCall)
            {
                cWriter.GenerateVirtualCall(
                    opCodeMethodInfo,
                    methodInfo,
                    thisType,
                    opCodeFirstOperand,
                    resultOfFirstOperand,
                    ref requiredType);
            }

            methodInfo.WriteFunctionCallLoadFunctionAddress(
                opCodeMethodInfo,
                thisType,
                ref methodAddressResultNumber,
                cWriter);

            if (cWriter.ProcessPluggableMethodCall(opCodeMethodInfo, methodInfo))
            {
                return;
            }

            if (!isIndirectMethodCall)
            {
                methodInfo.WriteFunctionNameExpression(methodAddressResultNumber, ownerOfExplicitInterface, cWriter);
            }

            methodInfo.GetParameters()
                .WriteFunctionCallArguments(
                    opCodeMethodInfo,
                    resultOfFirstOperand,
                    isVirtual,
                    hasThis,
                    isCtor,
                    thisResultNumber,
                    thisType,
                    methodInfo.ReturnType,
                    cWriter,
                    methodInfo.CallingConvention.HasFlag(CallingConventions.VarArgs));
        }

        /// <summary>
        /// </summary>
        /// <param name="cWriter">
        /// </param>
        /// <param name="opCode">
        /// </param>
        /// <param name="fromResult">
        /// </param>
        /// <param name="toType">
        /// </param>
        /// <param name="throwExceptionIfNull">
        /// </param>
        /// <returns>
        /// </returns>
        public static bool WriteCast(
            this CWriter cWriter,
            OpCodePart opCode,
            OpCodePart opCodeOperand,
            IType toType,
            bool throwExceptionIfNull = false)
        {
            var writer = cWriter.Output;

            var estimatedOperandResultOf = cWriter.EstimatedResultOf(opCodeOperand);

            var bareType = !estimatedOperandResultOf.Type.IsArray
                ? estimatedOperandResultOf.Type.ToBareType()
                : estimatedOperandResultOf.Type;

            var isNull = estimatedOperandResultOf.Type.IsPointer && estimatedOperandResultOf.Type.GetElementType().IsVoid();

            if (toType.IsInterface && !isNull)
            {
                if (bareType.GetAllInterfaces().Contains(toType))
                {
                    writer.Write("&");
                    cWriter.WriteInterfaceAccess(opCodeOperand, bareType, toType);
                }
                else
                {
                    return cWriter.WriteDynamicCast(writer, opCode, opCodeOperand, toType, throwExceptionIfNull: throwExceptionIfNull);
                }
            }
            else if (estimatedOperandResultOf.Type.IntTypeBitSize() == CWriter.PointerSize * 8 &&
                     (toType.IsPointer || toType.IsByRef))
            {
                WriteCCast(cWriter, opCodeOperand, toType);
            }
            else if (estimatedOperandResultOf.Type.IsArray
                     || (estimatedOperandResultOf.Type.IsPointer && bareType.TypeEquals(cWriter.System.System_Void))
                     || toType.IsArray 
                     || toType.IsPointer 
                     || toType.IsByRef 
                     || bareType.IsDerivedFrom(toType))
            {
                WriteCCast(cWriter, opCodeOperand, toType);
            }
            else
            {
                Debug.Assert(estimatedOperandResultOf.Type.IntTypeBitSize() == 0);
                return cWriter.WriteDynamicCast(writer, opCode, opCodeOperand, toType, throwExceptionIfNull: throwExceptionIfNull);
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="cWriter">
        /// </param>
        /// <param name="opCode">
        /// </param>
        /// <param name="typeToSave">
        /// </param>
        /// <param name="operandIndex">
        /// </param>
        /// <param name="destination">
        /// </param>
        public static void WriteSave(
            this CWriter cWriter,
            OpCodePart opCode,
            IType typeToSave,
            int operandIndex,
            FullyDefinedReference destination,
            bool destinationIsIndirect = false)
        {
            var writer = cWriter.Output;

            if (destinationIsIndirect)
            {
                writer.Write("*(");
            }
            
            cWriter.WriteResult(destination);
            if (destinationIsIndirect)
            {
                writer.Write(")");
            }

            writer.Write(" = ");

            if (!cWriter.AdjustToType(opCode.OpCodeOperands[operandIndex], typeToSave))
            {
                cWriter.WriteOperandResultOrActualWrite(writer, opCode, operandIndex);
            }
        }

        public static void WriteLlvmSavePrimitiveIntoStructure(
            this CWriter cWriter,
            OpCodePart opCode,
            FullyDefinedReference source,
            FullyDefinedReference destination)
        {
            var writer = cWriter.Output;

            writer.WriteLine("; Copy primitive data into a structure");

            // write access to a field
            if (cWriter.WriteFieldAccess(
                opCode,
                destination.Type.ToClass(),
                destination.Type.ToClass(),
                0,
                destination) == null)
            {
                writer.WriteLine("; No data");
                return;
            }

            writer.WriteLine(string.Empty);

            cWriter.SaveToField(opCode, opCode.Result.Type, 0);

            writer.WriteLine(string.Empty);
            writer.WriteLine("; End of Copy primitive data");
        }

        /// <summary>
        /// </summary>
        /// <param name="cWriter">
        /// </param>
        /// <param name="type">
        /// </param>
        /// <param name="op1">
        /// </param>
        /// <param name="op2">
        /// </param>
        public static void WriteMemCopy(
            this CWriter cWriter,
            IType type,
            FullyDefinedReference op1,
            FullyDefinedReference op2)
        {
            var writer = cWriter.Output;

            writer.WriteLine(
                "memcpy({0}, {1}, {2})",
                op1,
                op2,
                type.GetTypeSize(cWriter));
        }

        public static void WriteMemCopy(
            this CWriter cWriter,
            OpCodePart op1,
            OpCodePart op2,
            OpCodePart size)
        {
            var writer = cWriter.Output;

            writer.Write("memcpy(");
            cWriter.WriteResultOrActualWrite(writer, op1);
            writer.Write(", ");
            cWriter.WriteResultOrActualWrite(writer, op2);
            writer.Write(", ");
            cWriter.WriteResultOrActualWrite(writer, size);
            writer.Write(")");
        }

        /// <summary>
        /// </summary>
        /// <param name="cWriter">
        /// </param>
        /// <param name="type">
        /// </param>
        /// <param name="op1">
        /// </param>
        public static void WriteMemSet(this CWriter cWriter, IType type, OpCodePart op1)
        {
            var writer = cWriter.Output;

            writer.Write("memset((Byte*) (");
            cWriter.WriteResultOrActualWrite(writer, op1);
            writer.Write("), 0, sizeof(");
            type.WriteTypePrefix(cWriter);
            writer.Write("))");
        }

        public static void WriteMemSet(this CWriter cWriter, OpCodePart op1, OpCodePart size)
        {
            var writer = cWriter.Output;

            writer.Write("memset((Byte*) (");
            cWriter.WriteResultOrActualWrite(writer, op1);
            writer.Write("), 0, (");
            cWriter.WriteResultOrActualWrite(writer, size);
            writer.Write("))");
        }

        public static void WriteMemSet(
            this CWriter cWriter,
            OpCodePart reference,
            OpCodePart init,
            OpCodePart size)
        {
            var writer = cWriter.Output;
            writer.Write("memset((Byte*) (");
            cWriter.WriteResultOrActualWrite(writer, reference);
            writer.Write("), ");
            cWriter.WriteResultOrActualWrite(writer, init);
            writer.Write(", (");
            cWriter.WriteResultOrActualWrite(writer, size);
            writer.Write("))");
        }
    }
}