﻿#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Secp256k1.Musig
{
#if SECP256K1_LIB
	public
#else
	internal
#endif
	record MusigPubNonce
	{
#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly GE K1, K2;

		internal MusigPubNonce(ECPubKey k1, ECPubKey k2)
		{
			this.K1 = k1.Q;
			this.K2 = k2.Q;
		}

		private MusigPubNonce(GE k1, GE k2)
		{
			this.K1 = k1.Normalize();
			this.K2 = k2.Normalize();
		}

		public static MusigPubNonce Aggregate(MusigPubNonce[] nonces)
		{
			if (nonces == null)
				throw new ArgumentNullException(nameof(nonces));
			if (nonces.Length is 0)
				throw new ArgumentException(nameof(nonces), "nonces should have at least one element");
			Span<GEJ> summed_nonces = stackalloc GEJ[2];
			secp256k1_musig_sum_nonces(summed_nonces, nonces);
			return new MusigPubNonce(summed_nonces[0].ToGroupElement(),
									 summed_nonces[1].ToGroupElement());
		}

		internal static void secp256k1_musig_sum_nonces(Span<GEJ> summed_nonces, MusigPubNonce[] pubnonces)
		{
			int i;
			summed_nonces[0] = GEJ.Infinity;
			summed_nonces[1] = GEJ.Infinity;

			for (i = 0; i < pubnonces.Length; i++)
			{
				summed_nonces[0] = summed_nonces[0].AddVariable(pubnonces[i].K1);
				summed_nonces[1] = summed_nonces[1].AddVariable(pubnonces[i].K2);
			}
		}

		public MusigPubNonce(ReadOnlySpan<byte> in66)
		{
			if (!TryParseGE(in66.Slice(0, 33), out K1) ||
				!TryParseGE(in66.Slice(33, 33), out K2))
			{
				throw new ArgumentException("Invalid musig pubnonce");
			}
			K1 = K1.Normalize();
			K2 = K2.Normalize();
		}

		private bool TryParseGE(ReadOnlySpan<byte> pub , out GE ge)
		{
			if (GE.TryParse(pub, out _, out ge))
				return true;
			if (pub.Length == 33 && pub.SequenceCompareTo(stackalloc byte[33]) == 0)
				return true;
			return false;
		}

		public void WriteToSpan(Span<byte> out66)
		{
			if (K1.IsInfinity)
				out66.Slice(0, 33).Fill(0);
			else
				new ECPubKey(K1, null).WriteToSpan(true, out66.Slice(0, 33), out _);
			if (K2.IsInfinity)
				out66.Slice(33, 33).Fill(0);
			else
				new ECPubKey(K2, null).WriteToSpan(true, out66.Slice(0, 33), out _);
		}

		public byte[] ToBytes()
		{
			byte[] b = new byte[66];
			WriteToSpan(b);
			return b;
		}
	}
}
#endif
