
// Copyright Christophe Bertrand.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using UniversalSerializerLib3.NumberTools;

// ###############################################################
// ###############################################################
// ###############################################################
// ###############################################################

namespace UniversalSerializerLib3.FileTools
{

	// ###############################################################
	// ###############################################################

	/// <summary>
	/// Allows use of protected Read7BitEncodedInt().
	/// </summary>
	internal class BinaryReader2 : BinaryReader
	{
		public BinaryReader2(Stream input)
			: base(input)
		{ }

		public BinaryReader2(Stream input, Encoding encoding)
			: base(input,encoding)
		{	}

#if NET3_5
		public new void Dispose(bool b)
		{
			base.Dispose(b); // protected method.
		}
		public void Dispose()
		{
			base.Dispose(true);
		}
#endif

		internal void SetPosition(long NewPosition)
		{
			if (base.BaseStream.Position != NewPosition)
				base.BaseStream.Position = NewPosition;
		}


		/// <summary>
		/// Renvoie -1 si on est à la fin du flux.
		/// Pour binaryformatter, qui nécessite un accès sans exception.
		/// </summary>
		/// <returns></returns>
		internal int ReadByteNoException()
		{
			return base.BaseStream.ReadByte();
		} 

		/// <summary>
		/// Reads compressed short from stream.
		/// </summary>
		/// <returns></returns>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal short Read7BitEncodedShort()
		{
			return unchecked((short)this.Read7BitEncodedUShort());
		}

		/// <summary>
		/// Reads compressed short from stream using special encoding.
		/// </summary>
		/// <returns></returns>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal short ReadSpecial7BitEncodedShort()
		{
			return unchecked(Numbers.Special16ToInt16((short)this.Read7BitEncodedUShort()));
		}

		/// <summary>
		/// Reads compressed ushort from stream.
		/// </summary>
		/// <returns></returns>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal ushort Read7BitEncodedUShort()
		{
			ushort retValue = 0;
			int shifter = 0;

			while (shifter != 21) // 21 because 7*3 > 16 (bits).
			{
				byte b = this.ReadByte();
				retValue |= (ushort)((ushort)(b & 0x7f) << shifter);
				shifter += 7;
				if ((b & 0x80) == 0)
				{
					return retValue;
				}
			}
#if DEBUG
			throw new FormatException();
#else
			return 0;
#endif
		}

		/// <summary>
		/// Reads a compressed integer from the stream.
		/// </summary>
		/// <returns></returns>
		public new int Read7BitEncodedInt()
		{
			return base.Read7BitEncodedInt(); // go to the protected method.
		}

		/// <summary>
		/// Reads a compressed integer from the stream using special encoding.
		/// </summary>
		/// <returns></returns>
		public int ReadSpecial7BitEncodedInt()
		{
			return Numbers.Special32ToInt32(base.Read7BitEncodedInt()); // go to the protected method.
		}

		/// <summary>
		/// Reads a compressed unsigned integer from the stream.
		/// </summary>
		/// <returns></returns>
		public uint Read7BitEncodedUInt()
		{
			return unchecked((uint)base.Read7BitEncodedInt()); // go to the protected method.
		}

		/// <summary>
		/// Reads compressed long from stream.
		/// </summary>
		/// <returns></returns>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal long Read7BitEncodedLong()
		{
			return unchecked((long)this.Read7BitEncodedULong());
		}

		/// <summary>
		/// Reads compressed long from stream special encoding.
		/// </summary>
		/// <returns></returns>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal long ReadSpecial7BitEncodedLong()
		{
			return Numbers.Special64ToInt64( unchecked((long)this.Read7BitEncodedULong()));
		}

		/// <summary>
		/// Reads compressed long from stream.
		/// </summary>
		/// <returns></returns>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal ulong Read7BitEncodedULong()
		{
			ulong retValue = 0;
			int shifter = 0;

			while (shifter != 70) // 70 because 7*10 > 64 (bits).
			{
				byte b = this.ReadByte();
				retValue |= (ulong)(b & 0x7f) << shifter;
				shifter += 7;
				if ((b & 0x80) == 0)
				{
					return retValue;
				}
			}
#if DEBUG
			throw new FormatException();
#else
			return 0;
#endif
		}



#if (SILVERLIGHT || PORTABLE) && !WINDOWS_PHONE8
		public decimal ReadDecimal()
        {
            int[] buffer = new int[4];// d.lo, d.mid, d.hi, d.flags            
            buffer[0] = this.ReadInt32();
            buffer[1] = this.ReadInt32();
            buffer[2] = this.ReadInt32();
            buffer[3] = this.ReadInt32();
            return new decimal(buffer);
        }
#endif

	}

	/// <summary>
	/// Allows use of protected Write7BitEncodedInt().
	/// </summary>
	internal class BinaryWriter2 : BinaryWriter
	{
		public BinaryWriter2(Stream output)
			: base(output)
		{ }
		public BinaryWriter2(Stream output, Encoding encoding)
			: base(output, encoding)
		{ }

#if NET3_5
		public new void Dispose(bool b)
		{
			base.Dispose(b); // protected method.
		}
		public void Dispose()
		{
			base.Dispose(true);
		}
#endif

		/// <summary>
		/// Writes compressed short to stream.
		/// </summary>
		/// <param name="value"></param>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void Write7BitEncodedShort(short value)
		{
			this.Write7BitEncodedUShort(unchecked((ushort)value));
		}

		/// <summary>
		/// Writes compressed short to stream using special encoding.
		/// </summary>
		/// <param name="value"></param>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void WriteSpecial7BitEncodedShort(short value)
		{
			this.Write7BitEncodedUShort(unchecked((ushort) Numbers.Int16ToSpecial16(value)));
		}

		/// <summary>
		/// Writes compressed ushort to stream.
		/// </summary>
		/// <param name="value"></param>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void Write7BitEncodedUShort(ushort value)
		{
			ushort number;

			for (number = value; number >= (ushort)128u; number >>= 7)
				this.Write(unchecked((byte)(number | (ushort)128u)));

			this.Write((byte)number); // remaining bits.
		}

		/// <summary>
		/// Writes a compressed integer to the stream.
		/// </summary>
		/// <param name="value"></param>
		public new void Write7BitEncodedInt(int value)
		{
			base.Write7BitEncodedInt(value); // go to the protected method.
		}

		/// <summary>
		/// Writes a compressed integer to the stream using special encoding.
		/// </summary>
		/// <param name="value"></param>
		public void WriteSpecial7BitEncodedInt(int value)
		{
			base.Write7BitEncodedInt(Numbers.Int32ToSpecial32(value)); // go to the protected method.
		}

		/// <summary>
		/// Writes a compressed unsigned integer to the stream.
		/// </summary>
		/// <param name="value"></param>
		public void Write7BitEncodedUInt(uint value)
		{
#if !WINDOWS_PHONE7_1
			base.Write7BitEncodedInt(unchecked((int)value)); // go to the protected method.
#else
			// I detected a bug in the platform, using the emulator of VS 2010 for Phone.
			// Here is a replacement for the erroneous function:
			uint number;

			for (number = value; number >= 128ul; number >>= 7)
				this.Write(unchecked((byte)(number | 128ul)));

			this.Write((byte)number); // remaining bits.
#endif
		}

		/// <summary>
		/// Writes compressed long to stream.
		/// </summary>
		/// <param name="value"></param>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void Write7BitEncodedLong(long value)
		{
			this.Write7BitEncodedULong(unchecked((ulong)value));
		}

		/// <summary>
		/// Writes compressed long to stream using special encoding.
		/// </summary>
		/// <param name="value"></param>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void WriteSpecial7BitEncodedLong(long value)
		{
			this.Write7BitEncodedULong(unchecked((ulong)Numbers.Int64ToSpecial64(value)));
		}

		/// <summary>
		/// Writes compressed ulong to stream.
		/// </summary>
		/// <param name="value"></param>
#if NET4_5
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
		internal void Write7BitEncodedULong(ulong value)
		{
			ulong number;

			for (number = value; number >= 128ul; number >>= 7)
				this.Write(unchecked((byte)(number | 128ul)));

			this.Write((byte)number); // remaining bits.
		}

#if (SILVERLIGHT || PORTABLE) && !WINDOWS_PHONE8
		public void WriteDecimal(decimal value)
        {
            var ints = decimal.GetBits(value); // returns d.lo, d.mid, d.hi, d.flags.
            foreach (int i in ints)
                this.Write(i);
        }
#endif

	}

	// ###############################################################
	// ###############################################################
	
#if NET3_5
	/// <summary>
	/// File tool functions.
	/// </summary>
	public static class File
	{
		#region From Mono
		/// <summary>
		/// Copy a stream content to another stream.
		/// Using a 16 kb buffer.
		/// </summary>
		/// <param name="This"></param>
		/// <param name="destination"></param>
		public static void CopyTo(this Stream This, Stream destination)
		{
			This.CopyTo(destination, 16 * 1024);
		}

		/// <summary>
		/// Copy a stream content to another stream.
		/// </summary>
		/// <param name="This"></param>
		/// <param name="destination"></param>
		/// <param name="bufferSize"></param>
		public static void CopyTo(this Stream This, Stream destination, int bufferSize)
		{
			if (destination == null)
				throw new ArgumentNullException("destination");
			if (!This.CanRead)
				throw new NotSupportedException("This stream does not support reading");
			if (!destination.CanWrite)
				throw new NotSupportedException("This destination stream does not support writing");
			if (bufferSize <= 0)
				throw new ArgumentOutOfRangeException("bufferSize");

			var buffer = new byte[bufferSize];
			int nread;
			while ((nread = This.Read(buffer, 0, bufferSize)) != 0)
				destination.Write(buffer, 0, nread);
		}
		#endregion From Mono
	}
#endif

	// ###############################################################
	// ###############################################################
}

	// ###############################################################
	// ###############################################################

// ###############################################################
// ###############################################################
#if false // Kept for possible future use.
namespace UniversalSerializerLib3.NumberTools
{

	internal static class Numbers
	{

		#region Special integers

		/* 
		 * 'Special' integers are signed integers where negative values have their bits reversed (except the higher bit),
		 * then value is rotated one bit left, the higher bit goes to the lower site.
		 * The objective is to reduce the number of set (1) bits for small negative numbers.
		 * The compresser considers small numbers (positive or negative) as more frequent and optimizes their storage size.
		 * 
		 * The transcoding method is the same in the two directions, but I wrote one function for each direction for clarity reasons.
		 */

		internal static short Int16ToSpecial16(short i)
		{
			if (i >= 0)
				return (short)(i << 1);
			return unchecked((short)((i << 1) ^ -1));
		}

		internal static short Special16ToInt16(short i)
		{
			if ((i & 1) == 0)
				return (short)((uint)(ushort)i >> 1);
			return unchecked((short)((((uint)(ushort)i >> 1)) ^ -1));
		}

		internal static int Int32ToSpecial32(int i)
		{
			if (i >= 0)
				return i << 1;
			return (i << 1) ^ -1;
		}

		internal static int Special32ToInt32(int i)
		{
			if ((i & 1) == 0)
				return (int)((uint)i >> 1);
			return ((int)((uint)i >> 1)) ^ -1;
		}

		internal static long Int64ToSpecial64(long i)
		{
			if (i >= 0)
				return i << 1;
			return (i << 1) ^ -1L;
		}

		internal static long Special64ToInt64(long i)
		{
			if ((i & 1) == 0)
				return (long)((ulong)i >> 1);
			return ((long)((ulong)i >> 1)) ^ -1L;
		}

		#endregion Special integers

	}
}

#endif
	// ###############################################################
	// ###############################################################
	// ###############################################################




