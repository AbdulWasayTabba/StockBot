"""
FMP's free tier has tight rate limits. Analyzing multiple symbols in parallel
(each needing 2-3 FMP calls) can burst past that limit and cause silent
failures. This semaphore caps how many FMP requests run concurrently across
the whole agent process.
"""

import asyncio

fmp_semaphore = asyncio.Semaphore(2)  # at most 2 concurrent FMP requests
