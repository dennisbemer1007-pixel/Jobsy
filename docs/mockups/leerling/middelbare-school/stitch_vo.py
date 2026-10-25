import sys; sys.path.insert(0,'/workspace/mocklib'); from stitch import build
S='/workspace/vo-mockup/illustraties'; O='/workspace/vo-mockup/src/img'
build(S,O,'kust',[('kust-p1.jpg',0,1024,355),('kust-p2.jpg',0,1024,345)],mobx=200)
build(S,O,'rif',[('rif-p1.jpg',0,1024,322),('rif-p2.jpg',0,1024,330)],mobx=900)
build(S,O,'grot',[('grot-p2.jpg',0,1024,300),('grot-p3.jpg',0,1024,300)],mobx=1400)
build(S,O,'eiland',[('eiland-p2.jpg',0,1024,345),('kust-p2.jpg',0,1024,345,1)],mobx=500)
build(S,O,'toren',[('toren-p2.jpg',0,1024,340,1),('toren-p2.jpg',0,1024,340)],mobx=900)
build(S,O,'lagune',[('lagune-p1.jpg',0,1024,345),('lagune-p2.jpg',0,1024,330)],mobx=350)
build(S,O,'route',[('route-p1.jpg',0,1024,345),('route-p2.jpg',0,1024,335)],mobx=150)
