export default function TestPrint() {
  return (
    <div style={{ padding: '20px', fontFamily: 'Arial' }}>
      <h1>TEST TRANG IN MỚI</h1>
      <p>Nếu bạn thấy trang này, nghĩa là route /test-print đã hoạt động!</p>
      <p>Thời gian: {new Date().toLocaleString('vi-VN')}</p>
      
      <button onClick={() => window.print()}>
        TEST IN
      </button>
      
      <style>{`
        @media print {
          body { font-size: 12px; }
          h1 { color: #000 !important; }
        }
      `}</style>
    </div>
  );
}