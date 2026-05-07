import { authFetch } from "@/lib/authFetch";
import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { AppLayout } from "@/components/layout/app-layout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useToast } from "@/hooks/use-toast";
import { apiRequest } from "@/lib/queryClient";
import { FileText, Send, TestTube, Settings } from "lucide-react";

export default function VNPTTestPage() {
  const { toast } = useToast();
  
  const [testConfig, setTestConfig] = useState({
    apiUrl: "http://your-vnpt-server.com:8080", // URL cá»§a VNPT server
    username: "",
    password: "",
    companyTaxCode: "",
    companyName: "",
    companyAddress: "",
    companyPhone: "",
    companyEmail: ""
  });

  const [testInvoice, setTestInvoice] = useState({
    buyerName: "KhÃ¡ch hÃ ng test",
    buyerTaxCode: "",
    buyerAddress: "HÃ  Ná»™i",
    buyerPhone: "0123456789",
    buyerEmail: "test@example.com",
    items: [
      {
        itemName: "Sáº£n pháº©m test",
        quantity: 1,
        unitPrice: 100000,
        taxRate: "10%"
      }
    ]
  });

  const [mttData, setMttData] = useState({
    account: "",
    acPass: "",
    pattern: "1/E-HOA_DON",
    serial: "E23TEA",
    convert: 0,
    fkey: "",
    xmlData: `<Inv>
  <key>INV001</key>
  <Invoice>
    <CusCode>KH001</CusCode>
    <CusName>KhÃ¡ch hÃ ng test MTT</CusName>
    <CusAddress>HÃ  Ná»™i</CusAddress>
    <Products>
      <Product>
        <ProdName>Sáº£n pháº©m MTT</ProdName>
        <ProdUnit>CÃ¡i</ProdUnit>
        <ProdQuantity>1</ProdQuantity>
        <ProdPrice>100000</ProdPrice>
        <Amount>100000</Amount>
      </Product>
    </Products>
    <Total>100000</Total>
  </Invoice>
</Inv>`
  });

  // Test authentication
  const testAuthMutation = useMutation({
    mutationFn: async () => {
      const response = await authFetch('/api/EInvoice/test-auth', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(testConfig)
      });
      return response.json();
    },
    onSuccess: (data) => {
      toast({
        title: "Test Authentication",
        description: data.success ? "ÄÄƒng nháº­p VNPT thÃ nh cÃ´ng!" : `Lá»—i: ${data.message}`,
        variant: data.success ? "default" : "destructive"
      });
    }
  });

  // Test create invoice
  const testCreateMutation = useMutation({
    mutationFn: async () => {
      // First save config
      await apiRequest('/api/EInvoice/config', {
        method: 'PUT',
        body: JSON.stringify({
          isEnabled: true,
          provider: "VNPT",
          apiUrl: testConfig.apiUrl,
          username: testConfig.username,
          password: testConfig.password,
          companyTaxCode: testConfig.companyTaxCode,
          companyName: testConfig.companyName,
          companyAddress: testConfig.companyAddress,
          companyPhone: testConfig.companyPhone,
          companyEmail: testConfig.companyEmail,
          defaultTemplate: "01GTKT0/001",
          defaultSymbol: "C22TKT",
          defaultTaxRate: "10%"
        }),
        headers: { 'Content-Type': 'application/json' }
      });

      // Then create test order and invoice
      const response = await authFetch('/api/EInvoice/test-create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(testInvoice)
      });
      return response.json();
    },
    onSuccess: (data) => {
      toast({
        title: "Test Create Invoice",
        description: data.success ? "Táº¡o hÃ³a Ä‘Æ¡n test thÃ nh cÃ´ng!" : `Lá»—i: ${data.message}`,
        variant: data.success ? "default" : "destructive"
      });
    }
  });

  // MTT Import and Publish
  const mttImportPublishMutation = useMutation({
    mutationFn: async () => {
      const response = await authFetch('/api/EInvoice/mtt/import-and-publish', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          account: mttData.account || testConfig.username,
          acpass: mttData.acPass || testConfig.password,
          xmlInvData: mttData.xmlData,
          username: testConfig.username,
          password: testConfig.password,
          pattern: mttData.pattern,
          serial: mttData.serial,
          convert: mttData.convert
        })
      });
      return response.json();
    },
    onSuccess: (data) => {
      toast({
        title: "MTT Import and Publish",
        description: data.success ? "Import vÃ  publish MTT thÃ nh cÃ´ng!" : `Lá»—i: ${data.message}`,
        variant: data.success ? "default" : "destructive"
      });
    }
  });

  // MTT Import by Pattern
  const mttImportPatternMutation = useMutation({
    mutationFn: async () => {
      const response = await authFetch('/api/EInvoice/mtt/import-by-pattern', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          account: mttData.account || testConfig.username,
          acpass: mttData.acPass || testConfig.password,
          xmlInvData: mttData.xmlData,
          username: testConfig.username,
          password: testConfig.password,
          pattern: mttData.pattern,
          serial: mttData.serial,
          convert: mttData.convert
        })
      });
      return response.json();
    },
    onSuccess: (data) => {
      toast({
        title: "MTT Import by Pattern",
        description: data.success ? "Import theo pattern MTT thÃ nh cÃ´ng!" : `Lá»—i: ${data.message}`,
        variant: data.success ? "default" : "destructive"
      });
    }
  });

  // MTT Send
  const mttSendMutation = useMutation({
    mutationFn: async () => {
      const response = await authFetch('/api/EInvoice/mtt/send', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          account: mttData.account || testConfig.username,
          acpass: mttData.acPass || testConfig.password,
          username: testConfig.username,
          password: testConfig.password,
          pattern: mttData.pattern,
          serial: mttData.serial,
          fkey: mttData.fkey
        })
      });
      return response.json();
    },
    onSuccess: (data) => {
      toast({
        title: "MTT Send",
        description: data.success ? "Gá»­i hÃ³a Ä‘Æ¡n MTT thÃ nh cÃ´ng!" : `Lá»—i: ${data.message}`,
        variant: data.success ? "default" : "destructive"
      });
    }
  });

  return (
    <AppLayout title="Test VNPT API">
      <div className="p-6 max-w-4xl mx-auto space-y-6">
        <div className="flex items-center gap-3">
          <TestTube className="w-8 h-8 text-blue-600" />
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Test VNPT API</h1>
            <p className="text-gray-600">Kiá»ƒm tra káº¿t ná»‘i vÃ  táº¡o hÃ³a Ä‘Æ¡n thá»­ nghiá»‡m vá»›i VNPT</p>
          </div>
        </div>

        {/* Configuration */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Settings className="w-5 h-5" />
              Cáº¥u hÃ¬nh VNPT
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  API URL *
                </label>
                <Input
                  value={testConfig.apiUrl}
                  onChange={(e) => setTestConfig(prev => ({ ...prev, apiUrl: e.target.value }))}
                  placeholder="http://your-vnpt-server.com:8080"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Username *
                </label>
                <Input
                  value={testConfig.username}
                  onChange={(e) => setTestConfig(prev => ({ ...prev, username: e.target.value }))}
                  placeholder="TÃ i khoáº£n VNPT"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Password *
                </label>
                <Input
                  type="password"
                  value={testConfig.password}
                  onChange={(e) => setTestConfig(prev => ({ ...prev, password: e.target.value }))}
                  placeholder="Máº­t kháº©u VNPT"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  MÃ£ sá»‘ thuáº¿ cÃ´ng ty *
                </label>
                <Input
                  value={testConfig.companyTaxCode}
                  onChange={(e) => setTestConfig(prev => ({ ...prev, companyTaxCode: e.target.value }))}
                  placeholder="0123456789"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                TÃªn cÃ´ng ty *
              </label>
              <Input
                value={testConfig.companyName}
                onChange={(e) => setTestConfig(prev => ({ ...prev, companyName: e.target.value }))}
                placeholder="TÃªn cÃ´ng ty Ä‘áº§y Ä‘á»§"
              />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Äá»‹a chá»‰ cÃ´ng ty
                </label>
                <Input
                  value={testConfig.companyAddress}
                  onChange={(e) => setTestConfig(prev => ({ ...prev, companyAddress: e.target.value }))}
                  placeholder="Äá»‹a chá»‰ cÃ´ng ty"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Sá»‘ Ä‘iá»‡n thoáº¡i
                </label>
                <Input
                  value={testConfig.companyPhone}
                  onChange={(e) => setTestConfig(prev => ({ ...prev, companyPhone: e.target.value }))}
                  placeholder="0123456789"
                />
              </div>
            </div>

            <div>
              <Button 
                onClick={() => testAuthMutation.mutate()}
                disabled={testAuthMutation.isPending || !testConfig.username || !testConfig.password}
                className="bg-green-600 hover:bg-green-700"
              >
                <Send className="w-4 h-4 mr-2" />
                {testAuthMutation.isPending ? "Äang test..." : "Test Authentication"}
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Test Invoice */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileText className="w-5 h-5" />
              HÃ³a Ä‘Æ¡n thá»­ nghiá»‡m
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  TÃªn khÃ¡ch hÃ ng
                </label>
                <Input
                  value={testInvoice.buyerName}
                  onChange={(e) => setTestInvoice(prev => ({ ...prev, buyerName: e.target.value }))}
                  placeholder="TÃªn khÃ¡ch hÃ ng"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  MÃ£ sá»‘ thuáº¿ khÃ¡ch hÃ ng
                </label>
                <Input
                  value={testInvoice.buyerTaxCode}
                  onChange={(e) => setTestInvoice(prev => ({ ...prev, buyerTaxCode: e.target.value }))}
                  placeholder="MST khÃ¡ch hÃ ng (tÃ¹y chá»n)"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Äá»‹a chá»‰
                </label>
                <Input
                  value={testInvoice.buyerAddress}
                  onChange={(e) => setTestInvoice(prev => ({ ...prev, buyerAddress: e.target.value }))}
                  placeholder="Äá»‹a chá»‰ khÃ¡ch hÃ ng"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Sá»‘ Ä‘iá»‡n thoáº¡i
                </label>
                <Input
                  value={testInvoice.buyerPhone}
                  onChange={(e) => setTestInvoice(prev => ({ ...prev, buyerPhone: e.target.value }))}
                  placeholder="0123456789"
                />
              </div>
            </div>

            <div>
              <Button 
                onClick={() => testCreateMutation.mutate()}
                disabled={testCreateMutation.isPending || !testConfig.companyTaxCode || !testConfig.companyName}
                className="bg-blue-600 hover:bg-blue-700"
              >
                <FileText className="w-4 h-4 mr-2" />
                {testCreateMutation.isPending ? "Äang táº¡o..." : "Táº¡o hÃ³a Ä‘Æ¡n test"}
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* MTT APIs Test */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Settings className="w-5 h-5" />
              Test MTT APIs (MÃ¡y tÃ­nh tiá»n)
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Account MTT
                </label>
                <Input
                  value={mttData.account}
                  onChange={(e) => setMttData(prev => ({ ...prev, account: e.target.value }))}
                  placeholder="TÃ i khoáº£n MTT (Ä‘á»ƒ trá»‘ng = dÃ¹ng username)"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  AC Password MTT
                </label>
                <Input
                  type="password"
                  value={mttData.acPass}
                  onChange={(e) => setMttData(prev => ({ ...prev, acPass: e.target.value }))}
                  placeholder="Máº­t kháº©u MTT (Ä‘á»ƒ trá»‘ng = dÃ¹ng password)"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Pattern
                </label>
                <Input
                  value={mttData.pattern}
                  onChange={(e) => setMttData(prev => ({ ...prev, pattern: e.target.value }))}
                  placeholder="1/E-HOA_DON"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Serial
                </label>
                <Input
                  value={mttData.serial}
                  onChange={(e) => setMttData(prev => ({ ...prev, serial: e.target.value }))}
                  placeholder="E23TEA"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Convert (0/1)
                </label>
                <Input
                  type="number"
                  value={mttData.convert}
                  onChange={(e) => setMttData(prev => ({ ...prev, convert: parseInt(e.target.value) || 0 }))}
                  placeholder="0"
                  min="0"
                  max="1"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Fkey (cho Send)
                </label>
                <Input
                  value={mttData.fkey}
                  onChange={(e) => setMttData(prev => ({ ...prev, fkey: e.target.value }))}
                  placeholder="KhÃ³a hÃ³a Ä‘Æ¡n cho Send API"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                XML Data
              </label>
              <textarea
                className="w-full h-32 p-3 border border-gray-300 rounded-md resize-none font-mono text-sm"
                value={mttData.xmlData}
                onChange={(e) => setMttData(prev => ({ ...prev, xmlData: e.target.value }))}
                placeholder="XML data cho hÃ³a Ä‘Æ¡n MTT"
              />
            </div>

            <div className="flex gap-3 flex-wrap">
              <Button 
                onClick={() => mttImportPublishMutation.mutate()}
                disabled={mttImportPublishMutation.isPending || !testConfig.username || !testConfig.password}
                className="bg-purple-600 hover:bg-purple-700"
              >
                <Send className="w-4 h-4 mr-2" />
                {mttImportPublishMutation.isPending ? "Äang import..." : "Import & Publish MTT"}
              </Button>

              <Button 
                onClick={() => mttImportPatternMutation.mutate()}
                disabled={mttImportPatternMutation.isPending || !testConfig.username || !testConfig.password}
                className="bg-indigo-600 hover:bg-indigo-700"
              >
                <FileText className="w-4 h-4 mr-2" />
                {mttImportPatternMutation.isPending ? "Äang import..." : "Import by Pattern MTT"}
              </Button>

              <Button 
                onClick={() => mttSendMutation.mutate()}
                disabled={mttSendMutation.isPending || !testConfig.username || !testConfig.password || !mttData.fkey}
                className="bg-pink-600 hover:bg-pink-700"
              >
                <Send className="w-4 h-4 mr-2" />
                {mttSendMutation.isPending ? "Äang gá»­i..." : "Send MTT"}
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Instructions */}
        <Card>
          <CardHeader>
            <CardTitle>HÆ°á»›ng dáº«n test</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2 text-sm text-gray-600">
              <p><strong>Test cÆ¡ báº£n:</strong></p>
              <p>1. Äiá»n Ä‘áº§y Ä‘á»§ thÃ´ng tin cáº¥u hÃ¬nh VNPT (URL, username, password, mÃ£ sá»‘ thuáº¿)</p>
              <p>2. Click "Test Authentication" Ä‘á»ƒ kiá»ƒm tra káº¿t ná»‘i</p>
              <p>3. Náº¿u authentication thÃ nh cÃ´ng, Ä‘iá»n thÃ´ng tin khÃ¡ch hÃ ng test</p>
              <p>4. Click "Táº¡o hÃ³a Ä‘Æ¡n test" Ä‘á»ƒ táº¡o hÃ³a Ä‘Æ¡n thá»­ nghiá»‡m</p>
              
              <p className="mt-4"><strong>Test MTT APIs:</strong></p>
              <p>5. Äiá»n thÃ´ng tin MTT (account, pattern, serial, XML data)</p>
              <p>6. Click "Import & Publish MTT" Ä‘á»ƒ test import vÃ  publish tá»« mÃ¡y tÃ­nh tiá»n</p>
              <p>7. Click "Import by Pattern MTT" Ä‘á»ƒ test import theo pattern</p>
              <p>8. Äiá»n Fkey vÃ  click "Send MTT" Ä‘á»ƒ test gá»­i hÃ³a Ä‘Æ¡n</p>
              <p>9. Kiá»ƒm tra káº¿t quáº£ trong toast notification</p>
            </div>
          </CardContent>
        </Card>
      </div>
    </AppLayout>
  );
}
