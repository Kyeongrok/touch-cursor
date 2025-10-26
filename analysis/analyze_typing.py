#!/usr/bin/env python3
"""
TouchCursor 타이핑 로그 분석 스크립트

사용법:
    python analyze_typing.py <log_file.jsonl>
    python analyze_typing.py --all  # 모든 로그 파일 분석
"""

import json
import pandas as pd
import sys
from pathlib import Path
from datetime import datetime
import matplotlib.pyplot as plt
import seaborn as sns

# 한글 폰트 설정 (Windows)
try:
    plt.rcParams['font.family'] = 'Malgun Gothic'
    plt.rcParams['axes.unicode_minus'] = False
except:
    pass


def load_jsonl(file_path):
    """JSONL 파일 로드"""
    data = []
    with open(file_path, 'r', encoding='utf-8') as f:
        for line in f:
            try:
                data.append(json.loads(line))
            except:
                continue
    return pd.DataFrame(data)


def analyze_basic_stats(df):
    """기본 통계 분석"""
    print("=" * 60)
    print("📊 기본 통계")
    print("=" * 60)

    print(f"총 이벤트 수: {len(df):,}")
    print(f"세션 ID: {df['SessionId'].iloc[0] if len(df) > 0 else 'N/A'}")
    print(f"기간: {df['Timestamp'].min()} ~ {df['Timestamp'].max()}")

    if 'MarkedAsMistake' in df.columns:
        mistake_rate = df['MarkedAsMistake'].mean() * 100
        print(f"\n❌ 오타율: {mistake_rate:.2f}% ({df['MarkedAsMistake'].sum():,} / {len(df):,})")

    if 'RolloverDetected' in df.columns:
        rollover_rate = df['RolloverDetected'].mean() * 100
        print(f"⚡ 롤오버 발생률: {rollover_rate:.2f}% ({df['RolloverDetected'].sum():,} / {len(df):,})")

    print(f"\n⏱️  타이밍 통계:")
    print(f"  평균 ElapsedMs: {df['ElapsedMs'].mean():.2f}ms")
    print(f"  중앙값 ElapsedMs: {df['ElapsedMs'].median():.2f}ms")
    print(f"  표준편차: {df['ElapsedMs'].std():.2f}ms")

    if 'TimeSinceLastKey' in df.columns:
        print(f"\n⌨️  타이핑 속도:")
        print(f"  평균 키 간격: {df['TimeSinceLastKey'].mean():.2f}ms")
        print(f"  최소 키 간격: {df['TimeSinceLastKey'].min():.2f}ms")
        print(f"  최대 키 간격: {df['TimeSinceLastKey'].max():.2f}ms")


def analyze_problem_keys(df):
    """문제가 되는 키 분석"""
    print("\n" + "=" * 60)
    print("🔍 문제 키 분석")
    print("=" * 60)

    # 키별 빈도
    print("\n📈 가장 많이 사용된 키 조합 Top 10:")
    key_combos = df['ActivationKeyName'] + ' + ' + df['SourceKeyName']
    top_keys = key_combos.value_counts().head(10)
    for i, (key, count) in enumerate(top_keys.items(), 1):
        print(f"  {i:2d}. {key:20s} : {count:5d}회")

    # 오타가 있는 경우
    if 'MarkedAsMistake' in df.columns and df['MarkedAsMistake'].sum() > 0:
        print("\n❌ 오타가 많은 키 Top 10:")
        mistake_keys = df[df['MarkedAsMistake'] == True]
        mistake_combos = mistake_keys['ActivationKeyName'] + ' + ' + mistake_keys['SourceKeyName']
        top_mistakes = mistake_combos.value_counts().head(10)
        for i, (key, count) in enumerate(top_mistakes.items(), 1):
            total = key_combos[key_combos == key].count()
            rate = (count / total * 100) if total > 0 else 0
            print(f"  {i:2d}. {key:20s} : {count:3d}회 / {total:5d}회 = {rate:5.2f}%")


def analyze_timing_patterns(df):
    """타이밍 패턴 분석"""
    print("\n" + "=" * 60)
    print("⏱️  타이밍 패턴 분석")
    print("=" * 60)

    if 'MarkedAsMistake' not in df.columns:
        print("오타 레이블이 없어 분석할 수 없습니다.")
        return

    mistakes = df[df['MarkedAsMistake'] == True]
    correct = df[df['MarkedAsMistake'] == False]

    if len(mistakes) > 0:
        print(f"\n📊 ElapsedMs 비교:")
        print(f"  오타 평균: {mistakes['ElapsedMs'].mean():.2f}ms")
        print(f"  정상 평균: {correct['ElapsedMs'].mean():.2f}ms")
        diff = mistakes['ElapsedMs'].mean() - correct['ElapsedMs'].mean()
        print(f"  차이: {diff:+.2f}ms ({'오타가 더 빠름' if diff < 0 else '오타가 더 느림'})")

    # 롤오버와 오타의 관계
    if 'RolloverDetected' in df.columns:
        print(f"\n⚡ 롤오버와 오타의 관계:")
        rollover_mistakes = df[df['RolloverDetected'] == True]['MarkedAsMistake'].mean() * 100
        normal_mistakes = df[df['RolloverDetected'] == False]['MarkedAsMistake'].mean() * 100
        print(f"  롤오버 발생 시 오타율: {rollover_mistakes:.2f}%")
        print(f"  정상 입력 시 오타율: {normal_mistakes:.2f}%")


def analyze_key_sequences(df):
    """키 시퀀스 패턴 분석"""
    print("\n" + "=" * 60)
    print("🔗 키 시퀀스 패턴")
    print("=" * 60)

    if 'PreviousKey' not in df.columns or 'MarkedAsMistake' not in df.columns:
        print("키 시퀀스 데이터가 없어 분석할 수 없습니다.")
        return

    # 이전 키 → 현재 키 패턴
    df['KeyPair'] = df['PreviousKey'].fillna('(start)') + ' → ' + df['SourceKeyName']

    # 오타율이 높은 키 시퀀스
    pair_stats = df.groupby('KeyPair').agg({
        'MarkedAsMistake': ['count', 'sum', 'mean']
    }).round(4)
    pair_stats.columns = ['Count', 'Mistakes', 'MistakeRate']
    pair_stats = pair_stats[pair_stats['Count'] >= 5]  # 최소 5회 이상
    pair_stats = pair_stats.sort_values('MistakeRate', ascending=False)

    print("\n❌ 오타율이 높은 키 시퀀스 Top 10:")
    for i, (pair, row) in enumerate(pair_stats.head(10).iterrows(), 1):
        print(f"  {i:2d}. {pair:30s} : {row['MistakeRate']*100:5.2f}% ({int(row['Mistakes'])}/{int(row['Count'])})")


def analyze_time_of_day(df):
    """시간대별 패턴 분석"""
    print("\n" + "=" * 60)
    print("🕐 시간대별 패턴")
    print("=" * 60)

    df['Timestamp'] = pd.to_datetime(df['Timestamp'])
    df['Hour'] = df['Timestamp'].dt.hour

    hourly = df.groupby('Hour').agg({
        'SourceKeyName': 'count',
        'MarkedAsMistake': 'mean',
        'ElapsedMs': 'mean'
    }).round(2)
    hourly.columns = ['Count', 'MistakeRate', 'AvgElapsedMs']

    print("\n📅 시간대별 통계:")
    print("  시각 | 이벤트 수 | 오타율 | 평균 ElapsedMs")
    print("  " + "-" * 50)
    for hour, row in hourly.iterrows():
        print(f"  {hour:02d}시 | {int(row['Count']):8d} | {row['MistakeRate']*100:5.2f}% | {row['AvgElapsedMs']:6.2f}ms")


def generate_plots(df, output_dir='./plots'):
    """그래프 생성"""
    Path(output_dir).mkdir(exist_ok=True)

    # 1. ElapsedMs 분포
    plt.figure(figsize=(10, 6))
    if 'MarkedAsMistake' in df.columns:
        mistakes = df[df['MarkedAsMistake'] == True]
        correct = df[df['MarkedAsMistake'] == False]
        plt.hist([correct['ElapsedMs'], mistakes['ElapsedMs']],
                 bins=50, label=['정상', '오타'], alpha=0.7)
        plt.legend()
    else:
        plt.hist(df['ElapsedMs'], bins=50)
    plt.xlabel('ElapsedMs (활성화 키 후 경과 시간)')
    plt.ylabel('빈도')
    plt.title('ElapsedMs 분포')
    plt.savefig(f'{output_dir}/elapsed_distribution.png', dpi=150, bbox_inches='tight')
    print(f"\n📊 그래프 저장: {output_dir}/elapsed_distribution.png")

    # 2. 시간대별 오타율
    if 'MarkedAsMistake' in df.columns:
        df['Timestamp'] = pd.to_datetime(df['Timestamp'])
        df['Hour'] = df['Timestamp'].dt.hour
        hourly_mistakes = df.groupby('Hour')['MarkedAsMistake'].mean() * 100

        plt.figure(figsize=(12, 5))
        hourly_mistakes.plot(kind='bar')
        plt.xlabel('시간')
        plt.ylabel('오타율 (%)')
        plt.title('시간대별 오타율')
        plt.xticks(rotation=0)
        plt.tight_layout()
        plt.savefig(f'{output_dir}/hourly_mistakes.png', dpi=150, bbox_inches='tight')
        print(f"📊 그래프 저장: {output_dir}/hourly_mistakes.png")


def main():
    if len(sys.argv) < 2:
        print("사용법: python analyze_typing.py <log_file.jsonl>")
        print("        python analyze_typing.py --all")
        sys.exit(1)

    log_file = sys.argv[1]

    # 로그 파일 로드
    print(f"📂 로그 파일 로드 중: {log_file}")
    df = load_jsonl(log_file)

    if len(df) == 0:
        print("❌ 로그 데이터가 없습니다.")
        sys.exit(1)

    # 분석 실행
    analyze_basic_stats(df)
    analyze_problem_keys(df)
    analyze_timing_patterns(df)
    analyze_key_sequences(df)
    analyze_time_of_day(df)

    # 그래프 생성
    try:
        generate_plots(df)
    except Exception as e:
        print(f"\n⚠️  그래프 생성 실패: {e}")

    print("\n" + "=" * 60)
    print("✅ 분석 완료!")
    print("=" * 60)


if __name__ == '__main__':
    main()
